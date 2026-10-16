using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BluewaterHaul;
public sealed class WorldData {
 public int Version {get;set;} public float[] Bounds {get;set;}=[];
 public List<Road> Roads {get;set;}=[]; public List<Building> Buildings {get;set;}=[];
 public List<Area> Areas {get;set;}=[]; public List<Poi> Pois {get;set;}=[]; public List<float[]> Trees {get;set;}=[];
 public static WorldData Load(string path)=>JsonSerializer.Deserialize<WorldData>(File.ReadAllText(path),Options)!;
 public static readonly JsonSerializerOptions Options=new(){PropertyNameCaseInsensitive=true,WriteIndented=false};
}
public sealed class Road {public long Id{get;set;} public string Name{get;set;}=""; public string Kind{get;set;}="";public float Width{get;set;} public List<float[]> Points{get;set;}=[];public bool Bridge{get;set;}public string Layer{get;set;}="0";public string Sidewalk{get;set;}="";public string Surface{get;set;}="";public string Speed{get;set;}="";public string Oneway{get;set;}="no"; public bool Driveable=>Kind is not ("footway" or "path" or "steps" or "cycleway" or "pedestrian");}
public sealed class Building {public long Id{get;set;}public string Name{get;set;}="";public string Address{get;set;}="";public string Kind{get;set;}="";public float Height{get;set;}public List<float[]> Points{get;set;}=[];public float[] Center{get;set;}=[];public bool Enterable{get;set;} public List<List<float[]>> RoofTriangles{get;set;}=[];public string BuildingType{get;set;}="yes";public bool HeightKnown{get;set;}public string Style="";public string Landmark="";public Vector2 Door;public RectangleF Box;public string Label=>Landmark=="lawrence"?"Lawrence House":Landmark=="college"?"Lambton College building":Landmark=="hospital"?"Bluewater Health":Name.Length>0?Name:Address.Length>0?Address:Kind=="yes"?"Neighbourhood building":Kind.Replace('_',' ');}
public sealed class Area {public List<List<float[]>> Holes{get;set;}=[];public List<List<float[]>> Triangles{get;set;}=[];public long Id{get;set;}public string Name{get;set;}="";public string Kind{get;set;}="";public List<float[]> Points{get;set;}=[];}
public sealed class Poi {public long Id{get;set;}public string Name{get;set;}="";public string Kind{get;set;}="";public float[] Point{get;set;}=[];}
public readonly record struct RectangleF(float X,float Y,float Width,float Height){public bool Contains(Vector2 p)=>p.X>=X&&p.X<=X+Width&&p.Y>=Y&&p.Y<=Y+Height;}
public sealed class Landmark {public string Name{get;set;}="";public float[] Point{get;set;}=[];public string Note{get;set;}="";public string Source{get;set;}="";public string Accuracy{get;set;}="OSM";}
public static class Geo {
 public static Vector2 V(float[] p)=>new(p[0],p[1]);
 public static Vector3 V3(Vector2 p,float y)=>new(p.X,y,p.Y);
 public static Vector2 Nearest(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return a+d*Math.Clamp(Vector2.Dot(p-a,d)/Math.Max(.001f,d.LengthSquared()),0,1);}
 public static bool Contains(List<float[]> p,Vector2 q){bool hit=false;int j=p.Count-1;for(int i=0;i<p.Count;i++){var a=V(p[i]);var b=V(p[j]);if((a.Y>q.Y)!=(b.Y>q.Y)&&q.X<(b.X-a.X)*(q.Y-a.Y)/(b.Y-a.Y)+a.X)hit=!hit;j=i;}return hit;}
 public static List<int> Triangulate(List<float[]> p){
  var result=new List<int>();if(p.Count<3)return result;
  var ids=Enumerable.Range(0,p.Count).ToList();float signed=0;for(int i=0;i<p.Count;i++){var a=V(p[i]);var b=V(p[(i+1)%p.Count]);signed+=a.X*b.Y-b.X*a.Y;}if(signed<0)ids.Reverse();
  int guard=p.Count*p.Count;
  float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;
  while(ids.Count>2&&guard-->0){bool cut=false;for(int i=0;i<ids.Count;i++){
   int ia=ids[(i+ids.Count-1)%ids.Count],ib=ids[i],ic=ids[(i+1)%ids.Count];var a=V(p[ia]);var b=V(p[ib]);var c=V(p[ic]);if(Cross(b-a,c-b)<=.001f)continue;
   bool inner=false;foreach(int k in ids){if(k==ia||k==ib||k==ic)continue;var q=V(p[k]);if(Cross(b-a,q-a)>.001f&&Cross(c-b,q-b)>.001f&&Cross(a-c,q-c)>.001f){inner=true;break;}}
   if(inner)continue;result.AddRange([ia,ib,ic]);ids.RemoveAt(i);cut=true;break;
  }if(!cut)break;}return result;
 }
}
public sealed class MeshBuilder {
 public readonly List<VertexPositionColor> Vertices=[];
 public void Tri(Vector3 a,Vector3 b,Vector3 c,Color color){Vertices.Add(new(a,color));Vertices.Add(new(b,color));Vertices.Add(new(c,color));}
 public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){Tri(a,b,c,color);Tri(a,c,d,color);}
 public void Box(Vector3 min,Vector3 max,Color color){
  Vector3 a=new(min.X,min.Y,min.Z),b=new(max.X,min.Y,min.Z),c=new(max.X,min.Y,max.Z),d=new(min.X,min.Y,max.Z);
  var up=new Vector3(0,max.Y-min.Y,0);
  Quad(a,b,b+up,a+up,color);Quad(b,c,c+up,b+up,color*.8f);Quad(c,d,d+up,c+up,color*.7f);Quad(d,a,a+up,d+up,color*.9f);Quad(a+up,b+up,c+up,d+up,color*1.1f);
 }
 public void Polygon(List<float[]> p,float y,Color color){var t=Geo.Triangulate(p);for(int i=0;i<t.Count;i+=3)Tri(Geo.V3(Geo.V(p[t[i]]),y),Geo.V3(Geo.V(p[t[i+1]]),y),Geo.V3(Geo.V(p[t[i+2]]),y),color);}
}
public sealed class WorldRenderer:IDisposable {
 sealed class Chunk {public List<VertexPositionColor> Data=[];public VertexBuffer? Buffer;}
 readonly Dictionary<(int,int),Chunk> chunks=[];readonly GraphicsDevice gd;
 public int VisibleChunks;public int TotalVertices;
 public WorldRenderer(GraphicsDevice gd,WorldData world){
  this.gd=gd;
  void Add(MeshBuilder mesh){for(int i=0;i<mesh.Vertices.Count;i+=3){var c=(mesh.Vertices[i].Position+mesh.Vertices[i+1].Position+mesh.Vertices[i+2].Position)/3;var key=((int)MathF.Floor(c.X/256),(int)MathF.Floor(c.Z/256));if(!chunks.TryGetValue(key,out var chunk))chunks[key]=chunk=new();chunk.Data.AddRange(mesh.Vertices.GetRange(i,3));TotalVertices+=3;}}
  foreach(var a in world.Areas){Color color=a.Kind switch{"water"=>new(50,133,163),"sand" or "beach"=>new(205,190,145),"wood" or "forest"=>new(83,112,69),"retail" or "commercial" or "industrial"=>new(157,154,144),"parking"=>new(109,111,108),_=>new(119,142,92)};float surface=a.Kind switch{"water"=>.09f,"parking"=>.07f,"sand" or "beach"=>.055f,"retail" or "commercial" or "industrial"=>.04f,"wood" or "forest"=>.01f,_=>.025f};var m=new MeshBuilder();if(a.Triangles.Count>0){foreach(var t in a.Triangles)m.Tri(Geo.V3(Geo.V(t[0]),surface),Geo.V3(Geo.V(t[1]),surface),Geo.V3(Geo.V(t[2]),surface),color);}else m.Polygon(a.Points,surface,color);Add(m);}
  foreach(var r in world.Roads){var m=new MeshBuilder();float y=r.Bridge?Math.Max(.5f,float.TryParse(r.Layer,out float lay)?lay*3:3):r.Driveable?.16f:.11f;for(int i=1;i<r.Points.Count;i++){
   var a=Geo.V(r.Points[i-1]);var b=Geo.V(r.Points[i]);var dir=b-a;if(dir.LengthSquared()<.1f)continue;dir.Normalize();var off=new Vector2(-dir.Y,dir.X)*r.Width/2;
   m.Quad(Geo.V3(a-off,StreetScene.Deck(r,a,y)),Geo.V3(b-off,StreetScene.Deck(r,b,y)),Geo.V3(b+off,StreetScene.Deck(r,b,y)),Geo.V3(a+off,StreetScene.Deck(r,a,y)),r.Driveable?new(75,79,80):new(183,177,157));
   if(r.Driveable&&r.Width>=7){var s=new Vector2(-dir.Y,dir.X)*.09f;for(float d=0;d<(b-a).Length();d+=12){var q=a+dir*d;var e=a+dir*Math.Min(d+4,(b-a).Length());m.Quad(Geo.V3(q-s,StreetScene.Deck(r,q,y)+.035f),Geo.V3(e-s,StreetScene.Deck(r,e,y)+.035f),Geo.V3(e+s,StreetScene.Deck(r,e,y)+.035f),Geo.V3(q+s,StreetScene.Deck(r,q,y)+.035f),new(211,191,103));}}
  }Add(m);}
  var renderedFootprints=new HashSet<string>();foreach(var b in world.Buildings){var footprint=string.Join(";",b.Points.Select(p=>((int)MathF.Round(p[0]*100),(int)MathF.Round(p[1]*100))).OrderBy(p=>p.Item1).ThenBy(p=>p.Item2));if(!renderedFootprints.Add(footprint))continue;var m=new MeshBuilder();Architecture.Building(m,b);Add(m);}
  foreach(var mesh in StreetScene.Build(world))Add(mesh);
  foreach(var t in world.Trees){var m=new MeshBuilder();var p=Geo.V(t);m.Box(Geo.V3(p-new Vector2(.18f),0),Geo.V3(p+new Vector2(.18f),2.8f),new(106,79,51));m.Box(Geo.V3(p-new Vector2(1.6f),2.4f),Geo.V3(p+new Vector2(1.6f),6),new(63,100,65));Add(m);}
 }
 public void Draw(BasicEffect effect,Vector2 position){VisibleChunks=0;foreach(var (key,c) in chunks){var center=new Vector2(key.Item1*256+128,key.Item2*256+128);if(Vector2.DistanceSquared(center,position)>1250*1250)continue;
  if(c.Buffer==null){c.Buffer=new(gd,typeof(VertexPositionColor),c.Data.Count,BufferUsage.WriteOnly);c.Buffer.SetData(c.Data.ToArray());c.Data.Clear();}
  gd.SetVertexBuffer(c.Buffer);foreach(var pass in effect.CurrentTechnique.Passes){pass.Apply();gd.DrawPrimitives(PrimitiveType.TriangleList,0,c.Buffer.VertexCount/3);}VisibleChunks++;
 }}
 public void Dispose(){foreach(var c in chunks.Values)c.Buffer?.Dispose();}
}
