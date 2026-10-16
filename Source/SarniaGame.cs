using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text.Json;
namespace BluewaterHaul;
public sealed class SaveState {
 public TroubleState Trouble{get;set;}=new();
 public List<VehicleState> Vehicles{get;set;}=[];public int ActiveVehicle{get;set;}=0;public bool ThirdPerson{get;set;}=true;
 public float[]? Waypoint{get;set;}public string WaypointName{get;set;}="Waypoint";public float X{get;set;}=-4930;public float Z{get;set;}=1200;public float TruckX{get;set;}=-4930;public float TruckZ{get;set;}=1200;public float Yaw{get;set;}=0;public float TruckYaw{get;set;}=0;public bool Driving{get;set;}=true;public float Fuel{get;set;}=100;public int Money{get;set;}=250;public int Job{get;set;}=0;public bool JobActive{get;set;}=true;public float Hour{get;set;}=9;public long InteriorId{get;set;}=0;public float ReturnX{get;set;}public float ReturnZ{get;set;}public Dictionary<long,int> Visits{get;set;}=[];public HashSet<long> Collected{get;set;}=[];public HashSet<int> Completed{get;set;}=[];
}
public sealed class Delivery {public string Title{get;set;}="";public string Cargo{get;set;}="";public string Destination{get;set;}="";public float[] Point{get;set;}=[];public int Pay{get;set;}public string Joke{get;set;}="";}
public sealed partial class SarniaGame:Game {
 readonly GraphicsDeviceManager graphics;readonly bool smoke;WorldData world=null!;WorldRenderer renderer=null!;BasicEffect effect=null!;SpriteBatch sprites=null!;Texture2D pixel=null!,font=null!;RenderTarget2D mapTexture=null!;
 List<Landmark> landmarks=[];List<Delivery> jobs=[];SaveState state=new();KeyboardState previous;MouseState lastMouse;Vector2 position,truck;float yaw,pitch,speed,cabLook;bool driving=true,map,journal,help=true,dialog;long interior;Vector2 returnPosition;Building? activeBuilding;MeshBuilder indoor=new();readonly Dictionary<(int,int),List<Building>> buildingGrid=[];readonly Dictionary<(int,int),List<Area>> waterGrid=[];
 float elapsed,noticeTime;string notice="Welcome to Bluewater Haul. Your first load is already aboard.";int frames;float roomW,roomD;Vector2 resident,item;string residentName="";bool itemTaken;string savePath="";string currentStreet="";float currentLimit=50;float roadTimer;int viewW,viewH;
 public SarniaGame(bool smoke=false){this.smoke=smoke;graphics=new(this){PreferredBackBufferWidth=1440,PreferredBackBufferHeight=900,SynchronizeWithVerticalRetrace=true,PreferredDepthStencilFormat=DepthFormat.Depth24};Window.Title="Bluewater Haul - Sarnia prototype 0.7";Window.AllowUserResizing=true;IsMouseVisible=true;IsFixedTimeStep=true;TargetElapsedTime=TimeSpan.FromSeconds(1d/60);}
 protected override void LoadContent(){
  world=WorldData.Load(Path.Combine(AppContext.BaseDirectory,"Data","world.json"));landmarks=JsonSerializer.Deserialize<List<Landmark>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Data","landmarks.json")),WorldData.Options)!;jobs=JsonSerializer.Deserialize<List<Delivery>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Data","jobs.json")),WorldData.Options)!;
  PlaceIdentity.Prepare(world);InitializeNavigation();renderer=new(GraphicsDevice,world);effect=new(GraphicsDevice){VertexColorEnabled=true,LightingEnabled=false,FogEnabled=true,FogStart=400,FogEnd=1150};sprites=new(GraphicsDevice);pixel=new(GraphicsDevice,1,1);pixel.SetData([Color.White]);font=Texture2D.FromFile(GraphicsDevice,Path.Combine(AppContext.BaseDirectory,"Data","font.png"));BuildStreetSigns();
  foreach(var b in world.Buildings){for(int x=(int)MathF.Floor(b.Box.X/128);x<=(int)MathF.Floor((b.Box.X+b.Box.Width)/128);x++)for(int z=(int)MathF.Floor(b.Box.Y/128);z<=(int)MathF.Floor((b.Box.Y+b.Box.Height)/128);z++){if(!buildingGrid.TryGetValue((x,z),out var list))buildingGrid[(x,z)]=list=[];list.Add(b);}}
  foreach(var a in world.Areas.Where(a=>a.Kind=="water")){float x0=a.Points.Min(p=>p[0]),x1=a.Points.Max(p=>p[0]),z0=a.Points.Min(p=>p[1]),z1=a.Points.Max(p=>p[1]);for(int x=(int)MathF.Floor(x0/128);x<=(int)MathF.Floor(x1/128);x++)for(int z=(int)MathF.Floor(z0/128);z<=(int)MathF.Floor(z1/128);z++){if(!waterGrid.TryGetValue((x,z),out var list))waterGrid[(x,z)]=list=[];list.Add(a);}}
  savePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BluewaterHaul","save-v1.json");
  if(smoke)savePath=Path.GetFullPath("verification/smoke-save.json");
  position=FindRoad(new(-4930,1200));yaw=-MathHelper.PiOver2;state.TruckYaw=yaw;truck=position;returnPosition=position;noticeTime=8;
  InitializeVehicles();UpdateRoad();BuildMap();if(File.Exists(savePath)&&!smoke)LoadSave();if(state.Waypoint==null)SetWaypoint(Geo.V(jobs[state.Job].Point),jobs[state.Job].Destination);InitializeStreetLife();InitializeTrouble();lastMouse=Mouse.GetState();
 }
 readonly string captureDirectory=Path.Combine("verification","captures-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
 void Capture(string name){Directory.CreateDirectory(captureDirectory);using var texture=new RenderTarget2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height,false,SurfaceFormat.Color,DepthFormat.Depth24);GraphicsDevice.SetRenderTarget(texture);DrawScene();GraphicsDevice.SetRenderTarget(null);using var stream=File.Create(Path.Combine(captureDirectory,name+".png"));texture.SaveAsPng(stream,texture.Width,texture.Height);if(stream.Length==0){var colors=new Color[texture.Width*texture.Height];texture.GetData(colors);using var staging=new Texture2D(GraphicsDevice,texture.Width,texture.Height);staging.SetData(colors);staging.SaveAsPng(stream,texture.Width,texture.Height);}if(stream.Length<8)throw new InvalidDataException("Screenshot encoding failed: "+name);stream.Flush(true);}
 Vector2 FindRoad(Vector2 p){float best=float.MaxValue;Vector2 q=p;foreach(var r in world.Roads.Where(r=>r.Driveable&&!r.Bridge))for(int i=1;i<r.Points.Count;i++){var n=Geo.Nearest(p,Geo.V(r.Points[i-1]),Geo.V(r.Points[i]));float d=Vector2.DistanceSquared(n,p);if(d<best){best=d;q=n;}}return q;}
 bool Press(Keys k,KeyboardState now)=>now.IsKeyDown(k)&&previous.IsKeyUp(k);
 void Tell(string s){notice=s;noticeTime=7;}
 protected override void Update(GameTime time){
  float dt=(float)time.ElapsedGameTime.TotalSeconds;frameDt=Math.Clamp(dt,.001f,.05f);elapsed+=dt;noticeTime=Math.Max(0,noticeTime-dt);var keys=Keyboard.GetState();var mouse=Mouse.GetState();
  if(Press(Keys.Escape,keys)){if(map&&searchFocus)searchFocus=false;else if(map)map=false;else if(journal)journal=false;else if(dialog)dialog=false;else if(help)help=false;else{Save();Exit();}}
  if(Press(Keys.F1,keys))help=!help;if(Press(Keys.M,keys)&&!searchFocus){map=!map;journal=false;help=false;dialog=false;}if(Press(Keys.J,keys)&&!searchFocus){journal=!journal;map=false;help=false;dialog=false;}
  if(Press(Keys.F5,keys))Save();if(Press(Keys.F9,keys))LoadSave();
  if(map)UpdateNavigationInput(keys,mouse);
  UpdateNavigation(dt);
  bool paused=map||journal||help||dialog;if(!paused){UpdateCrashPhysics(dt);UpdateStreetLife(dt);UpdateTrouble(dt);}IsMouseVisible=paused;
  if(!paused&&IsActive){
   if(Press(Keys.V,keys)){thirdPerson=!thirdPerson;cameraReady=false;}if(thirdPerson&&mouse.ScrollWheelValue!=lastMouse.ScrollWheelValue)cameraDistance=Math.Clamp(cameraDistance-(mouse.ScrollWheelValue-lastMouse.ScrollWheelValue)/120f,5,24);
   int cx=GraphicsDevice.Viewport.Width/2,cy=GraphicsDevice.Viewport.Height/2;
   if(lastMouse.X==cx&&lastMouse.Y==cy){if(driving)cabLook=Math.Clamp(cabLook-(mouse.X-cx)*.0028f,thirdPerson?-MathHelper.Pi:-1.3f,thirdPerson?MathHelper.Pi:1.3f);else yaw-=(mouse.X-cx)*.0028f;pitch=Math.Clamp(pitch-(mouse.Y-cy)*.0028f,thirdPerson?-.5f:-1.2f,thirdPerson?.6f:1.2f);}Mouse.SetPosition(cx,cy);
   if(driving){if(keys.IsKeyDown(Keys.Left))cabLook=Math.Clamp(cabLook+dt*1.4f,thirdPerson?-MathHelper.Pi:-1.3f,thirdPerson?MathHelper.Pi:1.3f);if(keys.IsKeyDown(Keys.Right))cabLook=Math.Clamp(cabLook-dt*1.4f,thirdPerson?-MathHelper.Pi:-1.3f,thirdPerson?MathHelper.Pi:1.3f);}else{if(keys.IsKeyDown(Keys.Left))yaw+=dt*1.4f;if(keys.IsKeyDown(Keys.Right))yaw-=dt*1.4f;}
   if(driving){DriveVehicle(keys,dt);
   }else{WalkPlayer(keys,dt);}

   state.Hour=(state.Hour+dt/120)%24;roadTimer-=dt;if(roadTimer<=0&&interior==0){UpdateRoad();roadTimer=.5f;}
   if(Press(Keys.X,keys))Vandalize();if(Press(Keys.K,keys))Repair();if(Press(Keys.B,keys))SurrenderOrPay();
   if(Press(Keys.E,keys))Interact();if(Press(Keys.Q,keys)){if(!state.JobActive){state.Job=(state.Job+1)%jobs.Count;state.JobActive=true;Tell("New fictional dispatch: "+jobs[state.Job].Title);}else Tell("Finish the current delivery before taking another load.");}
   if(Press(Keys.R,keys)&&interior==0){var p=world.Pois.FirstOrDefault(p=>p.Kind=="fuel"&&Vector2.Distance(position,Geo.V(p.Point))<45);if(p!=null){int price=(int)Math.Ceiling((100-ActiveFuel)*1.5);if(state.Money>=price){state.Money-=price;ActiveVehicle.Fuel=100;if(activeVehicle==0)state.Fuel=100;Tell("Tank filled. $"+price+". The coffee remains your own problem.");}else Tell("Not enough money to fill the tank.");}else Tell("Drive within 45 m of a mapped fuel station to refuel.");}
   if(Press(Keys.H,keys)&&interior==0)RecoverVehicle();
  }
  if(smoke){
   frames++;
   if(frames==90){help=false;Capture("street");}
   if(frames==110){help=false;map=false;journal=false;Interact();if(driving)throw new InvalidDataException($"Cab exit failed: speed={speed}, position={position}, notice={notice}, bounds={string.Join(",",world.Bounds)}");var b=world.Buildings.Where(b=>b.Enterable).MinBy(b=>Vector2.DistanceSquared(Geo.V(b.Center),position))!;returnPosition=position;Enter(b);position=resident+new Vector2(0,2.5f);yaw=0;}
   if(frames==140)Capture("interior");
   if(frames==150){position=item;Interact();if(!state.Collected.Contains(interior))throw new InvalidDataException("Interior collectible did not persist");Save();var id=interior;state.Collected.Clear();LoadSave();if(!state.Collected.Contains(id)||activeBuilding==null)throw new InvalidDataException("Interior save/load failed");Leave();map=true;}
   if(frames==155){SetWaypoint(Geo.V(jobs[0].Point),jobs[0].Destination);mapCamera.Center=OutsidePosition;mapCamera.Scale=.7f;}
   if(frames==175){Capture("map");if(route.Count<2)throw new InvalidDataException("Live dispatch road routing failed");}
   if(frames==178){map=false;Capture("navigation");}
   if(frames==180){map=false;interior=0;driving=false;state.Job=0;state.JobActive=true;position=Geo.V(jobs[0].Point);truck=position;int before=state.Money;Interact();if(state.JobActive||state.Money!=before+jobs[0].Pay)throw new InvalidDataException("Delivery payment failed");}
   if(frames==190){map=false;journal=true;Capture("journal");journal=false;help=true;}
   if(frames==210)Capture("help");
   if(frames==220)TourPlace("Sarnia City Hall");if(frames==225)Capture("cityhall");
   if(frames==230)TourPlace("Lawrence House");if(frames==235)Capture("lawrence");
   if(frames==240)TourPlace("Blue Water Bridge");if(frames==245)Capture("bridge");
   if(frames==250)TourPlace("Golden Hawk Memorial");if(frames==255)Capture("golden-hawk");
   if(frames==260)TourPlace("Canatara Park");if(frames==265)Capture("canatara");
   if(frames==270)TourPlace("Sarnia Library");if(frames==275)Capture("library");
   if(frames==280)VehicleSmoke();
   if(frames==282)StreetLifeSmoke();
   if(frames==284)TroubleSmoke();
   if(frames==285)PhysicsSmoke();
   if(frames==287){Console.WriteLine("PASS graphical smoke: cab exit, building entry, collectible, interior save/load, outdoor return, delivery handover/payment, map and UI renders.");Exit();}
  }
  previous=keys;lastMouse=Mouse.GetState();base.Update(time);
 }
 bool Blocked(Vector2 p){
  if(interior!=0){if(Math.Abs(p.X)>roomW/2-.45f||Math.Abs(p.Y)>roomD/2-.45f)return true;if(Math.Abs(p.Y)<.2f&&Math.Abs(p.X)>1.2f)return true;return false;}
  if(p.X<world.Bounds[0]+3||p.X>world.Bounds[2]-3||p.Y<world.Bounds[1]+3||p.Y>world.Bounds[3]-3)return true;
  var key=((int)MathF.Floor(p.X/128),(int)MathF.Floor(p.Y/128));if(buildingGrid.TryGetValue(key,out var list))foreach(var b in list)if(b.Box.Contains(p)&&Geo.Contains(b.Points,p))return true;
  if(waterGrid.TryGetValue(key,out var waters))foreach(var a in waters)if(Geo.Contains(a.Points,p)&&!a.Holes.Any(h=>Geo.Contains(h,p))){bool bridge=world.Roads.Any(r=>r.Bridge&&r.Points.Zip(r.Points.Skip(1),(aa,bb)=>Vector2.DistanceSquared(Geo.Nearest(p,Geo.V(aa),Geo.V(bb)),p)).Any(d=>d<100));if(!bridge)return true;}
  return false;
 }
 void UpdateRoad(){float best=60*60;currentStreet="Off road";currentLimit=50;foreach(var r in world.Roads){for(int i=1;i<r.Points.Count;i++){var n=Geo.Nearest(position,Geo.V(r.Points[i-1]),Geo.V(r.Points[i]));float d=Vector2.DistanceSquared(position,n);if(d<best){best=d;currentStreet=r.Name.Length>0?r.Name:r.Kind.Replace('_',' ');if(float.TryParse(r.Speed,out var s))currentLimit=s;}}}}
 (Building?,Vector2,float) NearBuilding(){Building? bestB=null;Vector2 door=default;float best=6*6;int cx=(int)MathF.Floor(position.X/128),cz=(int)MathF.Floor(position.Y/128);for(int x=cx-1;x<=cx+1;x++)for(int z=cz-1;z<=cz+1;z++){if(!buildingGrid.TryGetValue((x,z),out var list))continue;foreach(var b in list){if(!b.Enterable)continue;for(int i=0;i<b.Points.Count;i++){var p=Geo.Nearest(position,Geo.V(b.Points[i]),Geo.V(b.Points[(i+1)%b.Points.Count]));float d=Vector2.DistanceSquared(p,position);if(d<best){best=d;bestB=b;door=p;}}}}return(bestB,door,MathF.Sqrt(best));}
 void Interact(){
  if(interior!=0){if(position.Y>roomD/2-3){Leave();return;}if(Vector2.Distance(position,resident)<3){dialog=true;return;}if(Vector2.Distance(position,item)<2&&!itemTaken){itemTaken=true;state.Collected.Add(interior);state.Money+=15;Tell("You found $15 in a returned-tool deposit envelope. A fictional prototype collectible.");return;}Tell("Walk to the caretaker, the envelope, or the exit.");return;}
  if(driving){ExitVehicle();return;}
  if(state.JobActive&&Vector2.Distance(position,Geo.V(jobs[state.Job].Point))<35&&Vector2.Distance(truck,Geo.V(jobs[state.Job].Point))<70){state.Money+=jobs[state.Job].Pay;state.Completed.Add(state.Job);state.JobActive=false;Tell("Delivered: "+jobs[state.Job].Cargo+". $"+jobs[state.Job].Pay+". "+jobs[state.Job].Joke);return;}
  var nearbyVehicle=NearVehicle();if(nearbyVehicle!=null){EnterVehicle(nearbyVehicle);return;}
  if(NearStoppedTraffic() is {} stopped){TakeTraffic(stopped);return;}
  if(NearCitizen() is {} citizen){TalkCitizen(citizen);return;}
  var (building,door,_)=NearBuilding();if(building!=null){returnPosition=position;Enter(building);return;}Tell("E: cab, nearby building, or delivery destination.");
 }
 void Enter(Building b){activeBuilding=b;interior=b.Id;driving=false;speed=0;state.Visits.TryGetValue(b.Id,out int visits);state.Visits[b.Id]=visits+1;BuildInterior(b);position=new(0,roomD/2-2);yaw=0;pitch=0;Tell(b.Label+" - generated interior; real floor plan unknown.");}
 void Leave(){position=returnPosition;interior=0;activeBuilding=null;pitch=0;Tell("Back outside. Your truck is where you left it.");}
 void BuildInterior(Building b){
  // Persistent layout derives from OSM ID, not the visit counter or runtime Random seed.
  int seed=(int)(b.Id%int.MaxValue);var rng=new Random(seed);roomW=Math.Clamp(b.Box.Width,8,28);roomD=Math.Clamp(b.Box.Height,10,32);residentName=new[]{"Gord","Bev","Murray","Denise","Al","Pat"}[seed%6];resident=new(-roomW/3,-roomD/3);item=new(roomW/3,-roomD/3);itemTaken=state.Collected.Contains(b.Id);indoor=new();
  indoor.Box(new(-roomW/2,-.2f,-roomD/2),new(roomW/2,0,roomD/2),new(137,110,79));Color wall=new(208,203,181);indoor.Box(new(-roomW/2,0,-roomD/2),new(roomW/2,3.2f,-roomD/2+.15f),wall);indoor.Box(new(-roomW/2,0,-roomD/2),new(-roomW/2+.15f,3.2f,roomD/2),wall);indoor.Box(new(roomW/2-.15f,0,-roomD/2),new(roomW/2,3.2f,roomD/2),wall);
  indoor.Box(new(-roomW/2,0,roomD/2-.15f),new(-1.2f,3.2f,roomD/2),wall);indoor.Box(new(1.2f,0,roomD/2-.15f),new(roomW/2,3.2f,roomD/2),wall);
  indoor.Box(new(-roomW/2,0,-.12f),new(-1.2f,3.2f,.12f),wall);indoor.Box(new(1.2f,0,-.12f),new(roomW/2,3.2f,.12f),wall);
  indoor.Box(new(-roomW/2,3.2f,-roomD/2),new(roomW/2,3.35f,roomD/2),new(218,216,203));
  bool shop=b.Kind is not ("house" or "detached" or "apartments" or "yes");
  indoor.Box(new(-roomW/2+1,0,-roomD/2+1),new(-roomW/2+3,shop?1.1f:.65f,-roomD/2+4),shop?new(107,79,48):new(81,108,128));
  for(int i=0;i<3;i++){float x=roomW/2-1;float z=-roomD/2+2+i*2;indoor.Box(new(x-.6f,0,z),new(x,2.1f,z+1.4f),new(107,81,57));for(int j=0;j<3;j++)indoor.Box(new(x-.68f,.4f+j*.55f,z+.1f),new(x+.01f,.47f+j*.55f,z+1.3f),new(178+rng.Next(25),145,94));}
  indoor.Box(new(-2,0,roomD/4),new(2,.75f,roomD/4+1.2f),new(134,89,52));
 }
 void Save(){SaveVehicles();state.X=position.X;state.Z=position.Y;state.TruckX=truck.X;state.TruckZ=truck.Y;state.Yaw=yaw;state.Driving=driving;state.InteriorId=interior;state.ReturnX=returnPosition.X;state.ReturnZ=returnPosition.Y;Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);string tmp=savePath+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(state,WorldData.Options));File.Move(tmp,savePath,true);Tell("Saved: vehicle, delivery, interiors, visits and collected deposits.");}
 void LoadSave(){try{if(!File.Exists(savePath)){Tell("No saved game yet.");return;}var loaded=JsonSerializer.Deserialize<SaveState>(File.ReadAllText(savePath),WorldData.Options);if(loaded==null)throw new InvalidDataException("Empty save");bodyMotion.Clear();knockdowns.Clear();blasts.Clear();walkingVelocity=Vector2.Zero;state=loaded;position=new(state.X,state.Z);truck=new(state.TruckX,state.TruckZ);yaw=state.Yaw;driving=state.Driving;interior=state.InteriorId;returnPosition=new(state.ReturnX,state.ReturnZ);speed=0;state.Job=Math.Clamp(state.Job,0,jobs.Count-1);if(interior!=0){activeBuilding=world.Buildings.FirstOrDefault(b=>b.Id==interior);if(activeBuilding!=null)BuildInterior(activeBuilding);else{interior=0;position=returnPosition;}}RestoreVehicles();InitializeStreetLife();InitializeTrouble();Tell("Saved journey loaded.");}catch(Exception ex){Tell("Save could not be loaded: "+ex.Message);}}
 protected override void Draw(GameTime time){DrawScene();base.Draw(time);}
 void DrawScene(){
  viewW=GraphicsDevice.Viewport.Width;viewH=GraphicsDevice.Viewport.Height;Color sky=new(156,192,205);GraphicsDevice.Clear(interior!=0?new Color(41,38,35):sky);GraphicsDevice.DepthStencilState=DepthStencilState.Default;GraphicsDevice.RasterizerState=RasterizerState.CullNone;GraphicsDevice.BlendState=BlendState.Opaque;
  Vector3 eye=Geo.V3(position,(driving?ActiveSpec.Eye:1.7f)+(interior==0?GroundHeight(position):0));float viewYaw=yaw+(driving?cabLook:0);var forward=new Vector3(-MathF.Sin(viewYaw)*MathF.Cos(pitch),MathF.Sin(pitch),-MathF.Cos(viewYaw)*MathF.Cos(pitch));ApplyVehicleCamera(ref eye,ref forward);effect.View=Matrix.CreateLookAt(eye,eye+forward,Vector3.Up);effect.Projection=Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(75),viewW/(float)viewH,.5f,1400);effect.World=Matrix.Identity;effect.FogColor=sky.ToVector3();effect.FogEnabled=interior==0;
  if(interior==0){var ground=new MeshBuilder();ground.Quad(new(position.X-1600,0,position.Y-1600),new(position.X+1600,0,position.Y-1600),new(position.X+1600,0,position.Y+1600),new(position.X-1600,0,position.Y+1600),new(129,149,102));DrawMesh(ground);renderer.Draw(effect,position);
   var dynamic=new MeshBuilder();DrawVehicles(dynamic);DrawAvatar(dynamic);DrawStreetLife(dynamic);DrawTrouble(dynamic);DrawCrashEffects(dynamic);if(state.JobActive){var dest=Geo.V(jobs[state.Job].Point);if(Vector2.Distance(dest,position)<1100){dynamic.Box(Geo.V3(dest-new Vector2(.3f),0),Geo.V3(dest+new Vector2(.3f),15),new(222,173,63));}}
   if(state.Waypoint!=null){var wp=Geo.V(state.Waypoint);if(Vector2.Distance(wp,position)<1100&&Vector2.Distance(wp,position)>60)dynamic.Box(Geo.V3(wp-new Vector2(.18f),0),Geo.V3(wp+new Vector2(.18f),12),new(39,223,229));}
   DrawMesh(dynamic);
  }else{DrawMesh(indoor);var people=new MeshBuilder();people.Box(Geo.V3(resident-new Vector2(.25f),0),Geo.V3(resident+new Vector2(.25f),1.4f),new(133,48,42));people.Box(Geo.V3(resident-new Vector2(.19f),1.4f),Geo.V3(resident+new Vector2(.19f),1.8f),new(213,173,139));if(!itemTaken)people.Box(Geo.V3(item-new Vector2(.3f),.5f),Geo.V3(item+new Vector2(.3f),.7f),new(223,194,120));DrawMesh(people);}
  if(interior==0)DrawStreetSigns();sprites.Begin(samplerState:SamplerState.PointClamp);DrawHud();if(driving&&interior==0&&!map&&!journal&&!help){Panel(new(0,viewH-125,viewW,125),new(28,40,42));Text(ActiveSpec.Name.ToUpper()+" / "+(thirdPerson?"CHASE":"CAB"),30,viewH-105,Color.LightGray,2);Text($"{Math.Abs(speed)*3.6f:0} KM/H    FUEL {ActiveFuel:0}%    $ {state.Money}",30,viewH-67,Color.White,2);Line(new(viewW/2-70,viewH-10),new(viewW/2+70,viewH-10),new(87,92,90),8);}
  if(!map&&!journal&&!help&&!dialog)DrawNavigationHud();TroubleHud();if(map)DrawMap();if(journal)DrawJournal();if(help)DrawHelp();if(dialog)DrawDialog();sprites.End();
 }
 void DrawTruck(MeshBuilder m){ // model in local coordinates rotated to the saved parked heading
  var t=new MeshBuilder();t.Box(new(-1.1f,.45f,-2),new(1.1f,2.8f,1),new(154,55,41));t.Box(new(-1.2f,.7f,1),new(1.2f,3.1f,7),new(205,196,174));t.Box(new(-.95f,1.7f,-2.04f),new(.95f,2.5f,-2),new(66,117,138));foreach(float z in new[]{-1.2f,1.4f,5.8f})foreach(float x in new[]{-1.3f,1.0f})t.Box(new(x,.05f,z-.4f),new(x+.3f,.85f,z+.4f),new(36,37,37));var matrix=Matrix.CreateRotationY(state.TruckYaw)*Matrix.CreateTranslation(truck.X,GroundHeight(truck),truck.Y);foreach(var v in t.Vertices)m.Vertices.Add(new(Vector3.Transform(v.Position,matrix),v.Color));
 }
 void DrawMesh(MeshBuilder mesh){if(mesh.Vertices.Count==0)return;var data=mesh.Vertices.ToArray();foreach(var pass in effect.CurrentTechnique.Passes){pass.Apply();GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,data,0,data.Length/3);}}
 void DrawHud(){Panel(new(18,16,viewW-36,70),new(21,38,43,230));Text("BLUEWATER HAUL",34,28,new(236,193,91),2);Text(interior!=0?activeBuilding!.Label:PlaceIdentity.District(position)+" / "+currentStreet,34,57,Color.White,1.5f);Text($"{state.Hour:00.0} H   $ {state.Money}   FUEL {ActiveFuel:0}%",viewW-490,34,Color.White,1.2f);
  if(!map&&!journal&&!help&&!dialog){Text("+",viewW/2-6,viewH/2-10,Color.White,2);string prompt;
   if(interior!=0)prompt=position.Y>roomD/2-3?"E  EXIT TO STREET":Vector2.Distance(position,resident)<3?"E  TALK TO "+residentName:Vector2.Distance(position,item)<2&&!itemTaken?"E  COLLECT DEPOSIT":"Explore the generated interior";
   else if(driving)prompt="W/S throttle / brake   A/D steer   E exit   SPACE brake   V camera";
   else if(state.JobActive&&Vector2.Distance(position,Geo.V(jobs[state.Job].Point))<35)prompt="E  DELIVER CARGO (truck must be nearby)";
   else if(NearVehicle() is {} nearby)prompt="E  ENTER "+VehicleSpec.For(nearby.Kind).Name.ToUpper();else if(NearStoppedTraffic() is {} stopped)prompt="E  TAKE STOPPED "+VehicleSpec.For(stopped.Kind).Name.ToUpper();else if(NearCitizen() is {} citizen)prompt="E  TALK / "+citizen.Activity.ToUpper();else{var (b,_,_)=NearBuilding();prompt=b!=null?"E  ENTER "+b.Label:"WASD walk   SHIFT run   E interact";}
   Panel(new(18,viewH-180,viewW-36,36),new(21,38,43,220));Text(prompt,30,viewH-173,Color.White,1.5f);
   if(state.JobActive){var job=jobs[state.Job];float d=Vector2.Distance(interior==0?position:returnPosition,Geo.V(job.Point));Text(job.Title+" / "+job.Destination+" / "+(d/1000).ToString("0.00")+" KM",30,105,new(247,218,150),1.5f);}
   Text("M map   J journal   F1 help   F5 save   F9 load   R fuel   H recover   V camera",30,viewH-32,Color.White,1.3f);
  }if(noticeTime>0&&!help&&!map&&!journal){Panel(new(18,viewH-235,viewW-36,42),new(28,42,47,235));Text(notice,30,viewH-223,new(255,232,163),1.3f);}
 }
 void Panel(Rectangle r,Color c)=>sprites.Draw(pixel,r,c);
 void Text(string s,float x,float y,Color color,float scale=1.5f){float sx=x;foreach(char raw in s){char c=raw;if(c=='\n'){x=sx;y+=18*scale;continue;}if(c<32||c>126)c='?';int n=c-32;sprites.Draw(font,new Vector2(x,y),new Rectangle(n%16*12,n/16*20,12,20),color,0,Vector2.Zero,scale,SpriteEffects.None,0);x+=10*scale;}}
 void Wrap(string s,float x,float y,int width,Color color,float scale=1.5f){int cols=Math.Max(10,(int)(width/(10*scale)));var line="";foreach(var word in s.Split(' ')){if(line.Length+word.Length+1>cols){Text(line,x,y,color,scale);y+=22*scale;line="";}line+=word+" ";}if(line.Length>0)Text(line,x,y,color,scale);}
 void Line(Vector2 a,Vector2 b,Color c,float width=1){var d=b-a;sprites.Draw(pixel,a,null,c,MathF.Atan2(d.Y,d.X),Vector2.Zero,new Vector2(d.Length(),width),SpriteEffects.None,0);}
 Vector2 MapPoint(Vector2 p)=>new((p.X-world.Bounds[0])/(world.Bounds[2]-world.Bounds[0]),(p.Y-world.Bounds[1])/(world.Bounds[3]-world.Bounds[1]));
 void BuildMap(){mapTexture=new(GraphicsDevice,1024,1024);GraphicsDevice.SetRenderTarget(mapTexture);GraphicsDevice.Clear(new(122,144,104));sprites.Begin(samplerState:SamplerState.PointClamp);foreach(var a in world.Areas.Where(a=>a.Kind is "water" or "parking" or "park" or "pitch" or "industrial").OrderBy(a=>a.Kind=="water"?1:0)){var p=a.Points; // rasterize mapped surfaces with horizontal scan lines to the map texture
   for(int y=0;y<1024;y+=2){float wz=world.Bounds[1]+y/1024f*(world.Bounds[3]-world.Bounds[1]);var xs=new List<float>();for(int i=0,j=p.Count-1;i<p.Count;j=i++){var aa=p[i];var bb=p[j];if((aa[1]>wz)!=(bb[1]>wz))xs.Add(aa[0]+(wz-aa[1])*(bb[0]-aa[0])/(bb[1]-aa[1]));}foreach(var hole in a.Holes)for(int i=0,j=hole.Count-1;i<hole.Count;j=i++){var aa=hole[i];var bb=hole[j];if((aa[1]>wz)!=(bb[1]>wz))xs.Add(aa[0]+(wz-aa[1])*(bb[0]-aa[0])/(bb[1]-aa[1]));}xs.Sort();for(int i=0;i+1<xs.Count;i+=2){float x0=MapPoint(new(xs[i],wz)).X*1024,x1=MapPoint(new(xs[i+1],wz)).X*1024;Panel(new((int)x0,y,Math.Max(1,(int)(x1-x0)),2),a.Kind switch{"water"=>new(68,136,160),"parking"=>new(112,122,123),"industrial"=>new(151,146,132),"pitch"=>new(135,161,99),_=>new(94,143,98)});}}
  }foreach(var r in world.Roads)for(int i=1;i<r.Points.Count;i++)Line(MapPoint(Geo.V(r.Points[i-1]))*1024,MapPoint(Geo.V(r.Points[i]))*1024,r.Driveable?new(226,220,193):new(150,171,125),r.Width>12?2:1);
  foreach(var b in world.Buildings){var p=MapPoint(Geo.V(b.Center))*1024;Panel(new((int)p.X,(int)p.Y,2,2),new(82,78,65));}sprites.End();GraphicsDevice.SetRenderTarget(null);
 }
 void DrawMap()=>DrawNavigationMap();
 void DrawJournal(){Panel(new(25,100,viewW-50,viewH-145),new(26,42,47));Text("DISPATCH + FIELD NOTES",50,125,new(244,205,115),2);var job=jobs[state.Job];Text(state.JobActive?"CURRENT LOAD: "+job.Title:"DELIVERY COMPLETE / PRESS Q OUTSIDE FOR NEXT LOAD",50,175,Color.White,1.5f);Wrap("Cargo: "+job.Cargo+". Destination: "+job.Destination+". Payment: $"+job.Pay+". Park within 70 metres, leave the cab and walk within 35 metres to hand over the load.",50,220,viewW-110,Color.White,1.5f);
  Text("PLACES VISITED: "+state.Visits.Count+" / DELIVERIES COMPLETED: "+state.Completed.Count,50,330,new(184,215,198),1.5f);
  Wrap("The road and building positions come from your Sarnia map extract. Named places retain mapped names. Building heights, architecture, room layouts and all residents are generated. Jobs are fictional and do not describe actual operations at these places.",50,390,viewW-110,Color.White,1.4f);
  Wrap("Local character: lake freighters, bridge traffic, waterfront parks, arts venues, residential neighbourhoods and the industrial south. Point Edward is its own village. America remains scenery in this first Canada prototype.",50,505,viewW-110,Color.White,1.4f);
  Wrap("The prototype has no realistic suspension, articulated trailer physics, full border crossing, terrain elevation or complete regional economy yet. Interiors are persistent two-room approximations; press E near any Canadian-side building wall to enter.",50,610,viewW-110,new(202,202,181),1.25f);Text("J close / F5 save / F9 load",50,viewH-85,Color.White,1.5f);
 }
 void DrawHelp(){Panel(new(70,115,viewW-140,viewH-180),new(23,38,42));Text("WELCOME TO BLUEWATER HAUL",100,145,new(241,199,103),2.5f);Wrap("An original Canadian trucking and neighbourhood RPG. Sarnia and Point Edward are real; your dispatchers and questionable repairs are fictional.",100,215,viewW-220,Color.White,1.5f);
  string[] lines=["W/S accelerate, brake then reverse; WASD walk outside","A/D steer; mouse look; SPACE brake; SHIFT handbrake","E leave / enter vehicles, enter buildings, talk, deliver cargo","M searchable map / J dispatch journal / F1 this screen","R refuel near a fuel station / Q next load after completing one","V cab / chase camera; wheel chase distance; SHIFT run on foot","Map: wheel zoom, drag pan, click waypoint, TAB search; F6 landmark tour","X property damage / K vehicle repair / B surrender or pay fines"];
  for(int i=0;i<lines.Length;i++)Text(lines[i],100,310+i*37,Color.White,1.5f);Text("Your first load is aboard. M shows its destination. F1 begins your journey.",100,viewH-110,new(241,211,142),1.5f);
 }
 void DrawDialog(){Panel(new(60,viewH-340,viewW-120,270),new(23,36,39));Text(residentName+" / FICTIONAL CARETAKER",85,viewH-315,new(242,201,110),1.8f);
  var jokes=new[]{"I fixed the door with three hinges and optimism. The optimism is load-bearing.","My cousin says the house is haunted. My cousin also thinks the thermostat reads minds.","If you need directions, turn where the old shop used to be. No, the other old shop.","That truck looks dependable. So does my kettle, and it owes me two kitchens.","The bridge is impressive. My brother tried building one over his ditch. We call it the ramp now.","Welcome. Please wipe your boots. The carpet has survived three owners and one fondue incident."};Wrap(jokes[(int)(interior%jokes.Length)],85,viewH-260,viewW-180,Color.White,1.6f);Text("This resident and dialogue are fictional. ESC to return.",85,viewH-115,new(172,199,191),1.3f);
 }
 protected override void UnloadContent(){renderer?.Dispose();effect?.Dispose();mapTexture?.Dispose();font?.Dispose();pixel?.Dispose();sprites?.Dispose();base.UnloadContent();}
}
