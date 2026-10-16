using Microsoft.Xna.Framework;
namespace BluewaterHaul;
public sealed class RoadRouter {
 public record Edge(int To,float Cost,string Name);
 public record Segment(int A,int B,string Name,bool Forward,bool Backward);
 public readonly List<Vector2> Nodes=[];readonly List<List<Edge>> links=[];public readonly List<Segment> Segments=[];readonly Dictionary<(int,int),List<Segment>> segmentGrid=[];
 public RoadRouter(IEnumerable<Road> roads,bool walking=false){var ids=new Dictionary<(int,int),int>();int Node(Vector2 p){var k=((int)MathF.Round(p.X*100),(int)MathF.Round(p.Y*100));if(ids.TryGetValue(k,out int n))return n;ids[k]=Nodes.Count;Nodes.Add(p);links.Add([]);return Nodes.Count-1;}
 foreach(var r in roads.Where(r=>r.Driveable&&(!walking||r.Kind is not ("motorway" or "motorway_link"))))for(int i=1;i<r.Points.Count;i++){int a=Node(Geo.V(r.Points[i-1])),b=Node(Geo.V(r.Points[i]));if(a==b)continue;bool f=walking||r.Oneway!="-1",back=walking||r.Oneway is not ("yes" or "1" or "true");float d=Vector2.Distance(Nodes[a],Nodes[b]);string name=r.Name.Length>0?r.Name:r.Kind.Replace('_',' ');if(f)links[a].Add(new(b,d,name));if(back)links[b].Add(new(a,d,name));var seg=new Segment(a,b,name,f,back);Segments.Add(seg);var pa=Nodes[a];var pb=Nodes[b];for(int x=(int)MathF.Floor(Math.Min(pa.X,pb.X)/256);x<=(int)MathF.Floor(Math.Max(pa.X,pb.X)/256);x++)for(int y=(int)MathF.Floor(Math.Min(pa.Y,pb.Y)/256);y<=(int)MathF.Floor(Math.Max(pa.Y,pb.Y)/256);y++){if(!segmentGrid.TryGetValue((x,y),out var list))segmentGrid[(x,y)]=list=[];list.Add(seg);}}}
 public (Segment Segment,Vector2 Point) Snap(Vector2 p){float best=float.MaxValue;Segment s=Segments[0];Vector2 q=default;int cx=(int)MathF.Floor(p.X/256),cy=(int)MathF.Floor(p.Y/256);var checkedSegments=new HashSet<Segment>();for(int radius=0;radius<80;radius++){for(int x=cx-radius;x<=cx+radius;x++)for(int y=cy-radius;y<=cy+radius;y++){if(radius>0&&x!=cx-radius&&x!=cx+radius&&y!=cy-radius&&y!=cy+radius)continue;if(!segmentGrid.TryGetValue((x,y),out var list))continue;foreach(var e in list){if(!checkedSegments.Add(e))continue;var v=Geo.Nearest(p,Nodes[e.A],Nodes[e.B]);float d=Vector2.DistanceSquared(v,p);if(d<best){best=d;s=e;q=v;}}}float edge=Math.Min(Math.Min(p.X-(cx-radius)*256,(cx+radius+1)*256-p.X),Math.Min(p.Y-(cy-radius)*256,(cy+radius+1)*256-p.Y));if(best<=edge*edge)break;}return(s,q);}
 public List<Vector2> Route(Vector2 start,Vector2 end){var (s,sp)=Snap(start);var(t,tp)=Snap(end);var cost=new Dictionary<int,float>();var prev=new Dictionary<int,int>();var queue=new PriorityQueue<int,float>();var finish=new Dictionary<int,float>();
 void Seed(int n,float d){if(!cost.TryGetValue(n,out var old)||d<old){cost[n]=d;prev[n]=-1;queue.Enqueue(n,d+Vector2.Distance(Nodes[n],tp));}}
 if(s.Backward)Seed(s.A,Vector2.Distance(sp,Nodes[s.A]));if(s.Forward)Seed(s.B,Vector2.Distance(sp,Nodes[s.B]));if(t.Forward)finish[t.A]=Vector2.Distance(tp,Nodes[t.A]);if(t.Backward)finish[t.B]=Vector2.Distance(tp,Nodes[t.B]);
 float best=float.MaxValue;int goal=-1;bool direct=s==t&&((Vector2.Dot(tp-sp,Nodes[s.B]-Nodes[s.A])>=0&&s.Forward)||(Vector2.Dot(tp-sp,Nodes[s.B]-Nodes[s.A])<=0&&s.Backward));if(direct)best=Vector2.Distance(sp,tp);
 while(queue.TryDequeue(out int n,out float priority)){if(priority>=best)break;float c=cost[n];if(finish.TryGetValue(n,out float tail)&&c+tail<best){best=c+tail;goal=n;}foreach(var e in links[n]){float next=c+e.Cost;if(cost.TryGetValue(e.To,out float old)&&old<=next)continue;cost[e.To]=next;prev[e.To]=n;queue.Enqueue(e.To,next+Vector2.Distance(Nodes[e.To],tp));}}
 if(goal<0)return direct?[sp,tp]:[];var result=new List<Vector2>{tp};for(int n=goal;n>=0;n=prev[n])result.Add(Nodes[n]);result.Add(sp);result.Reverse();return result;}
}
public sealed class MapCamera {
 public Vector2 Center;public float Scale=1;
 public Vector2 Screen(Vector2 p,Rectangle r)=>new Vector2(r.Center.X,r.Center.Y)+(p-Center)*Scale;
 public Vector2 World(Vector2 p,Rectangle r)=>Center+(p-new Vector2(r.Center.X,r.Center.Y))/Scale;
 public void Zoom(Vector2 anchor,Rectangle r,float factor,float minimum){var before=World(anchor,r);Scale=Math.Clamp(Scale*factor,minimum,8);Center+=before-World(anchor,r);}
}
