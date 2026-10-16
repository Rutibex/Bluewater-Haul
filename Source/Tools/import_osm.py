#!/usr/bin/env python3
"""OSM PBF importer using Shapely for geometry; reads local extract without network."""
import struct,zlib,collections,json,math,sys,pathlib
B=(-82.423,42.942,-82.269,43.023); LAT=42.9825; LON=-82.346
SX=111320*math.cos(math.radians(LAT)); SZ=111320

def vi(b,i):
 v=s=0
 while True:
  x=b[i];i+=1;v|=(x&127)<<s
  if x<128:return v,i
  s+=7

def fields(b):
 i=0;d=collections.defaultdict(list)
 while i<len(b):
  k,i=vi(b,i);n=k>>3;w=k&7
  if w==0:v,i=vi(b,i)
  elif w==2:l,i=vi(b,i);v=b[i:i+l];i+=l
  elif w==1:v=b[i:i+8];i+=8
  elif w==5:v=b[i:i+4];i+=4
  else:raise ValueError('Unsupported protobuf wire type')
  d[n].append(v)
 return d

def packed(b):
 i=0;a=[]
 while i<len(b):v,i=vi(b,i);a.append(v)
 return a

def zz(v):return (v>>1)^-(v&1)
def project(lon,lat):return [round((lon-LON)*SX,2),round((LAT-lat)*SZ,2)]
def inside(lon,lat):return B[0]<=lon<=B[2] and B[1]<=lat<=B[3]
nodes={};ways=[];relations=[]
with open(sys.argv[1],'rb') as f:
 while x:=f.read(4):
  h=fields(f.read(struct.unpack('>I',x)[0]));blob=fields(f.read(h[3][0]));raw=blob[1][0] if 1 in blob else zlib.decompress(blob[3][0])
  if h[1][0]!=b'OSMData':continue
  pb=fields(raw);ss=[v.decode('utf8') for v in fields(pb[1][0])[1]];g=pb.get(17,[100])[0];la=pb.get(19,[0])[0];lo=pb.get(20,[0])[0]
  def tags(o):return {ss[k]:ss[v] for k,v in zip(packed(o.get(2,[b''])[0]),packed(o.get(3,[b''])[0]))}
  for grp in pb[2]:
   pg=fields(grp)
   for dense in pg.get(2,[]):
    de=fields(dense);ids=packed(de[1][0]);lats=packed(de[8][0]);lons=packed(de[9][0]);kv=packed(de.get(10,[b''])[0]);idx=a=b=ident=0
    for iv,av,bv in zip(ids,lats,lons):
     ident+=zz(iv);a+=zz(av);b+=zz(bv);t={}
     if idx<len(kv):
      while kv[idx]!=0:t[ss[kv[idx]]]=ss[kv[idx+1]];idx+=2
      idx+=1
     nodes[ident]=((lo+g*b)/1e9,(la+g*a)/1e9,t)
   for item in pg.get(1,[]):
    o=fields(item);nodes[o[1][0]]=((lo+g*zz(o[9][0]))/1e9,(la+g*zz(o[8][0]))/1e9,tags(o))
   for item in pg.get(3,[]):
    o=fields(item);acc=0;refs=[]
    for v in packed(o.get(8,[b''])[0]):acc+=zz(v);refs.append(acc)
    ways.append((o[1][0],refs,tags(o)))
   for item in pg.get(4,[]):
    o=fields(item);acc=0;members=[]
    for role,v,typ in zip(packed(o.get(8,[b''])[0]),packed(o.get(9,[b''])[0]),packed(o.get(10,[b''])[0])):
     acc+=zz(v);members.append((typ,acc,ss[role]))
    relations.append((o[1][0],members,tags(o)))

xmin,zmax=project(B[0],B[1]);xmax,zmin=project(B[2],B[3])
def clip(poly):
 for axis,val,greater in [(0,xmin,True),(0,xmax,False),(1,zmin,True),(1,zmax,False)]:
  out=[]
  if not poly:return []
  a=poly[-1]
  for b in poly:
   ia=a[axis]>=val if greater else a[axis]<=val;ib=b[axis]>=val if greater else b[axis]<=val
   if ia!=ib:
    t=(val-a[axis])/(b[axis]-a[axis]);out.append([a[0]+t*(b[0]-a[0]),a[1]+t*(b[1]-a[1])])
   if ib:out.append(b)
   a=b
  poly=out
 return poly

def area(p):return abs(sum(p[i-1][0]*p[i][1]-p[i][0]*p[i-1][1] for i in range(len(p))))/2
roadtypes={'motorway':20,'motorway_link':8,'trunk':14,'trunk_link':8,'primary':13,'primary_link':8,'secondary':11,'tertiary':9,'residential':7,'unclassified':7,'service':5,'living_street':6,'track':4,'footway':2,'path':2,'cycleway':2.5,'pedestrian':4,'steps':2}
roads=[];buildings=[];areas=[];pois=[];trees=[];missing=0
for ident,refs,t in ways:
 coords=[nodes[r][:2] for r in refs if r in nodes]
 if len(coords)!=len(refs):missing+=1;continue
 if len(coords)<2:continue
 p=[project(*c) for c in coords]
 if not any(inside(*c) for c in coords):continue
 if t.get('highway') in roadtypes:
  # Break outside segments; never join disconnected parts after clipping.
  runs=[];run=[]
  for i in range(len(p)-1):
   a,b=p[i],p[i+1];dx=b[0]-a[0];dz=b[1]-a[1];t0,t1=0.,1.;ok=True
   for q,r in [(-dx,a[0]-xmin),(dx,xmax-a[0]),(-dz,a[1]-zmin),(dz,zmax-a[1])]:
    if q==0:
     if r<0:ok=False
    elif q<0:t0=max(t0,r/q)
    else:t1=min(t1,r/q)
   if ok and t0<=t1:
    aa=[round(a[0]+dx*t0,2),round(a[1]+dz*t0,2)];bb=[round(a[0]+dx*t1,2),round(a[1]+dz*t1,2)]
    if run and run[-1]!=aa:runs.append(run);run=[]
    if not run:run=[aa]
    run.append(bb)
   elif run:runs.append(run);run=[]
  if run:runs.append(run)
  for run in runs:
   roads.append(dict(id=ident,name=t.get('name',t.get('ref','')),kind=t['highway'],width=roadtypes[t['highway']],points=run,bridge=t.get('bridge','no')!='no',layer=t.get('layer','0'),speed=t.get('maxspeed',''),oneway=t.get('oneway','no'),sidewalk=t.get('sidewalk',''),surface=t.get('surface','')))
 if 'building' in t and len(p)>=4 and refs[0]==refs[-1]:
  pp=clip(p[:-1]);ar=area(pp) if len(pp)>=3 else 0
  if ar<8:continue
  height=6.
  try:height=float(t.get('height',str(float(t.get('building:levels','2'))*3)).split()[0])
  except ValueError:pass
  center=[sum(v[0] for v in pp)/len(pp),sum(v[1] for v in pp)/len(pp)]
  label=t.get('name','');address=' '.join(t.get(k,'') for k in ['addr:housenumber','addr:street']).strip()
  buildings.append(dict(id=ident,name=label,address=address,kind=t.get('shop',t.get('amenity',t['building'])),height=max(3,min(70,height)),heightKnown=('height' in t or 'building:levels' in t),buildingType=t['building'],points=pp,center=center,enterable=(sum(c[0] for c in coords)/len(coords)>-82.414)))
 kind=t.get('natural',t.get('landuse',t.get('leisure', 'parking' if t.get('amenity')=='parking' else '')))
 if kind and refs[0]==refs[-1] and len(p)>3:
  pp=clip(p[:-1])
  if len(pp)>=3 and area(pp)>20:areas.append(dict(id=ident,name=t.get('name',''),kind=kind,points=pp))
 if t.get('name') and any(k in t for k in ['shop','amenity','tourism','leisure']):
  loc=next((c for c in coords if inside(*c)),None)
  if loc:pois.append(dict(id=ident,name=t['name'],kind=t.get('shop',t.get('amenity',t.get('tourism',t.get('leisure','')))),point=project(*loc)))
for ident,(lon,lat,t) in nodes.items():
 if not inside(lon,lat):continue
 if t.get('natural')=='tree':trees.append(project(lon,lat))
 if t.get('name') and any(k in t for k in ['shop','amenity','tourism','leisure','place']):pois.append(dict(id=ident,name=t['name'],kind=t.get('shop',t.get('amenity',t.get('tourism',t.get('leisure',t.get('place',''))))),point=project(lon,lat)))
# Recover water surfaces from the supplied relation shorelines, including partial
# large-lake relations. Polygonize clipped boundary lines against the extract box.
# The water-side sample points below are explicit regional interpretation, not
# a second map source. Complete small polygons preserve their inner islands.
from shapely.geometry import Polygon, LineString, box, Point
from shapely.ops import polygonize, unary_union
from shapely import make_valid, constrained_delaunay_triangles
clipbox=box(xmin,zmin,xmax,zmax)
lookup={i:(r,t) for i,r,t in ways}
water_samples={'Lake Huron':[project(-82.35,43.022)],'Saint Clair River':[project(-82.406,42.975),project(-82.417,42.992)],'Black River':[project(-82.420,42.980)]}
for ident,members,t in relations:
 if t.get('type')!='multipolygon' or t.get('natural')!='water':continue
 name=t.get('name',t.get('waterway:name',''))
 segments=[];inners=[]
 for typ,i,role in members:
  if typ!=1 or i not in lookup:continue
  refs=lookup[i][0]
  if any(r not in nodes for r in refs):continue
  pp=[project(*nodes[r][:2]) for r in refs]
  if role=='inner' and refs[0]==refs[-1] and len(pp)>3:inners.append(make_valid(Polygon(pp)))
  elif role in ('outer',''):segments.append(LineString(pp).intersection(clipbox))
 if not segments:continue
 faces=list(polygonize(unary_union(segments+[clipbox.boundary])))
 chosen=[]
 if name in water_samples:
  chosen=[face for face in faces if face.area<clipbox.area*.45 and any(face.covers(Point(q)) for q in water_samples[name])]
 else:
  # Complete ring interiors: choose faces which do not merely represent the
  # unused rectangle outside the relation's own closed rings.
  complete=[make_valid(Polygon([project(*nodes[r][:2]) for r in lookup[i][0]])) for typ,i,role in members if typ==1 and role in ('outer','') and i in lookup and lookup[i][0][0]==lookup[i][0][-1] and len(lookup[i][0])>3 and all(r in nodes for r in lookup[i][0])]
  chosen=[face for face in faces if any(poly.covers(face.representative_point()) for poly in complete)]
 for face in chosen:
  if inners:face=face.difference(unary_union(inners))
  parts=list(face.geoms) if hasattr(face,'geoms') else [face]
  for poly in parts:
   if poly.geom_type!='Polygon' or poly.area<20:continue
   outer=[[round(x,2),round(z,2)] for x,z in list(poly.exterior.coords)[:-1]]
   holes=[[[round(x,2),round(z,2)] for x,z in list(r.coords)[:-1]] for r in poly.interiors]
   triangles=[[[round(x,2),round(z,2)] for x,z in list(tri.exterior.coords)[:-1]] for tri in constrained_delaunay_triangles(poly).geoms]
   areas.append(dict(id=ident,name=name,kind='water',points=outer,holes=holes,triangles=triangles))
# Complete non-water multipolygons (including Centennial Park).
for ident,members,t in relations:
 kind=t.get('natural',t.get('landuse',t.get('leisure', 'parking' if t.get('amenity')=='parking' else '')))
 if t.get('type')!='multipolygon' or not kind or kind=='water':continue
 chains=[list(lookup[i][0]) for typ,i,role in members if typ==1 and role in ('outer','') and i in lookup]
 while chains:
  ring=chains.pop();changed=True
  while ring[0]!=ring[-1] and changed:
   changed=False
   for j,other in enumerate(chains):
    if ring[-1]==other[0]:ring+=other[1:]
    elif ring[-1]==other[-1]:ring+=other[-2::-1]
    elif ring[0]==other[-1]:ring=other[:-1]+ring
    elif ring[0]==other[0]:ring=other[:0:-1]+ring
    else:continue
    chains.pop(j);changed=True;break
  if ring[0]!=ring[-1] or any(r not in nodes for r in ring):continue
  face=make_valid(Polygon([project(*nodes[r][:2]) for r in ring[:-1]])).intersection(clipbox)
  parts=list(face.geoms) if hasattr(face,'geoms') else [face]
  for poly in parts:
   if poly.geom_type!='Polygon' or poly.area<20:continue
   areas.append(dict(id=ident,name=t.get('name',''),kind=kind,points=[[round(x,2),round(z,2)] for x,z in list(poly.exterior.coords)[:-1]]))
# Repair rare invalid / duplicate building rings before roof triangulation.
for b in buildings:
 poly=make_valid(Polygon(b['points']))
 if poly.geom_type=='MultiPolygon':poly=max(poly.geoms,key=lambda x:x.area)
 if poly.geom_type=='Polygon':
  b['points']=[[round(x,3),round(z,3)] for x,z in list(poly.exterior.coords)[:-1]]
  # Explicit triangulation includes all vertices while avoiding collinear ears.
  b['roofTriangles']=[[[round(x,3),round(z,3)] for x,z in list(tri.exterior.coords)[:-1]] for tri in constrained_delaunay_triangles(poly).geoms]
addresses=[dict(id=i,point=project(lon,lat),tags=t) for i,(lon,lat,t) in nodes.items() if inside(lon,lat) and 'addr:housenumber' in t]
# Assign named POIs inside unnamed building footprints.
def contains(p,q):
 hit=False;j=len(p)-1
 for i in range(len(p)):
  a,b=p[i],p[j]
  if (a[1]>q[1])!=(b[1]>q[1]) and q[0]<(b[0]-a[0])*(q[1]-a[1])/(b[1]-a[1])+a[0]:hit=not hit
  j=i
 return hit
# Address nodes are part of this same extract; attach only when inside a footprint.
for a in addresses:
 for b in buildings:
  if not b['address'] and abs(b['center'][0]-a['point'][0])<150 and abs(b['center'][1]-a['point'][1])<150 and contains(b['points'],a['point']):
   b['address']=' '.join(a['tags'].get(k,'') for k in ['addr:housenumber','addr:street']).strip()
   break
for poi in pois:
 for b in buildings:
  if abs(b['center'][0]-poi['point'][0])<250 and abs(b['center'][1]-poi['point'][1])<250 and contains(b['points'],poi['point']):
   if not b['name']:b['name']=poi['name'];b['kind']=poi['kind']
   break
world=dict(version=1,origin=[LON,LAT],bounds=[xmin,zmin,xmax,zmax],roads=roads,buildings=buildings,areas=areas,pois=pois,trees=trees,addresses=addresses)
out=pathlib.Path(sys.argv[2]);out.write_text(json.dumps(world,separators=(',',':')))
print(json.dumps(dict(roads=len(roads),buildings=len(buildings),areas=len(areas),pois=len(pois),trees=len(trees),missing_reference_ways=missing,bounds=world['bounds'])))
for p in pois:
 if any(s in p['name'].lower() for s in ['canatara','centennial','lawrence','alix','germain','lambton college','blue water','chipican','city hall','watson','wawanosh']):print(p)
