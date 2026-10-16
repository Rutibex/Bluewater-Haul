import json,math,pathlib
root=pathlib.Path(__file__).resolve().parents[1];w=json.loads((root/'Data/world.json').read_text())
def mean(points):return [sum(p[i] for p in points)/len(points) for i in range(2)]
def place(name):
 a=next((a for a in w['areas'] if a['name']==name),None)
 if a:return mean(a['points'])
 p=next((p for p in w['pois'] if p['name']==name),None)
 if p:return p['point']
 raise ValueError(name)
parks='https://www.sarnia.ca/play/parks-and-trails/'
landmarks=[]
def add(name,point,note,source,accuracy='Mapped OSM feature'):
 landmarks.append(dict(name=name,point=point,note=note,source=source,accuracy=accuracy))
add('Canatara Park',place('Canatara Park'),'Lake Huron beach, trails, pavilions, animal farm and miniature train. This prototype preserves mapped park geometry; attractions are research notes, not all modelled yet.',parks)
add('Centennial Park',place('Centennial Park'),'Sarnia Bay waterfront park with gardens, walking paths, the Agora and marina access. Delivery handover is on a nearby street, not a licence to drive trucks onto park lawns.',parks)
add('Point Edward Waterfront',place('Waterfront Park'),'Point Edward is a separate village. Its waterfront looks across the St. Clair River toward Port Huron and up at the Blue Water Bridge. Shipping and bridge traffic shape the setting.','https://www.villageofpointedward.com/community-services/parks')
bridge=[p for r in w['roads'] if r['name']=='Blue Water Bridge' for p in r['points']]
add('Blue Water Bridge',mean(bridge),'The original span opened in 1938 and a second span in 1997. The OSM alignment is real; generated bridge decks are placeholders. Border crossing is outside this prototype.','https://www.villageofpointedward.com/living-point-edward/history','OSM road alignment; generated structure')
add('Sarnia City Hall',place('Sarnia City Hall'),'Municipal landmark at 255 Christina Street North. Mapped footprint and position; generated exterior and interior.','https://www.sarnia.ca/public-notices/notice-of-adoption-of-a-new-official-plan/')
add('Sarnia Library',place('Lambton County Library - Sarnia'),'Mapped downtown library at 124 Christina Street South. The library is part of the downtown cultural cluster. Generated rooms are not its real floor plan.','https://www.sarnia.ca/app/uploads/2022/10/Sarnia-Official-Plan-2014-as-Amended-July-2022.pdf')
add('Lawrence House area',[-4960,1350],'127 Christina Street South: an 1892 Queen Anne house, now an arts centre. The address is verified; this marker is an approximate block location, not a verified building match. Local ghost stories remain stories.','https://lawrencehouse.ca/history-2/','Approximate address-area marker')
add('Germain Park',place('Germain Park'),'Recreation grounds with sports facilities, gardens, walking trails and the Golden Hawk memorial. The footprint is mapped; detailed attractions remain future work.',parks)
add('Lambton College',place('Lambton College'),'1457 London Road. College and London Road corridor provide a distinct eastern destination. The marker is mapped; campus interiors are generated.','https://www.lambtoncollege.ca/')
add('Wawanosh Wetlands',place('Wawanosh Wetlands Conservation Area'),'Mapped nature reserve. The nearby Nature Way offers wetland and woodland walking connections; only the paths present in this extract are represented.',parks)
add('Mitton Village',place('Mitton Village'),'A distinct neighbourhood and commercial area recognized by the City community improvement plan. Fictional local residents and errands can give this district its own social life.','https://www.sarnia.ca/community-improvement-plan/')
add('Bluewater Health',place('Bluewater Health - Sarnia'),'The hospital is mapped as a destination. It has a generated public interior, not an accurate hospital reconstruction; no clinical simulation is implemented.','https://en.wikipedia.org/wiki/Sarnia')
# Pass 2: curated identity corrections retain their evidence and uncertainty.
l=next(l for l in landmarks if l['name']=='Lawrence House area');l.update(name='Lawrence House',point=[-5095.26,1407.629],accuracy='Inferred corner/footprint match',note='127 Christina Street South: Queen Anne arts venue. Tower and chimney cues follow municipal heritage research. OSM footprint 1526503538 is an inferred match from the documented corner; exterior proportions are stylized.')
l=next(l for l in landmarks if l['name']=='Blue Water Bridge');l['note']='The north span has a cantilever truss; its south twin uses an arch structure. Both follow the supplied road alignments. Elevations and engineering dimensions are generated; international crossing is outside this prototype.'
add('Golden Hawk Memorial',[-3102.768,773.674],'Germain Park F-86 RCAF memorial, metallic gold and red colours. Original stylized model. Anchor from the official memorial listing.','https://www.veterans.gc.ca/en/remembrance/memorials/canada/sarnias-f-86-rcaf-memorial','Curated official memorial anchor')
(root/'Data/landmarks.json').write_text(json.dumps(landmarks,indent=2))
def nearest_road(p):
 best=1e30;out=None
 for r in w['roads']:
  if r['kind'] not in ['residential','tertiary','secondary','primary','unclassified'] or r['bridge']:continue
  for a,b in zip(r['points'],r['points'][1:]):
   dx=b[0]-a[0];dz=b[1]-a[1];t=max(0,min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/max(.001,dx*dx+dz*dz)))
   q=[a[0]+t*dx,a[1]+t*dz];d=sum((q[i]-p[i])**2 for i in range(2))
   if d<best:best=d;out=q
 return out
jobs=[]
for title,cargo,dest,pay,joke in [
 ('The Folding-Chair Situation','24 folding chairs, 23 of which fold','Centennial Park',180,'The twenty-fourth chair is now a sculpture.'),
 ('A Very Portable Picnic','Picnic supplies and an unreasonable quantity of napkins','Point Edward Waterfront',210,'The napkins have their own committee.'),
 ('The Lawn-Bowling Engineering Department','A crate of fictional club equipment','Germain Park',175,'Nobody agreed on which end of the level was up.'),
 ('Advanced Duct Tape Studies','Workshop supplies for a fictional student project','Lambton College',260,'It is a prototype. That explains the smoke.'),
 ('The Beach Umbrella Committee','Beach umbrellas for a fictional volunteer group','Canatara Park',230,'They voted to postpone the wind.')]:
 lm=next(l for l in landmarks if l['name']==dest);jobs.append(dict(title=title,cargo=cargo,destination=dest+' street-side handover',point=nearest_road(lm['point']),pay=pay,joke=joke))
(root/'Data/jobs.json').write_text(json.dumps(jobs,indent=2));print('Prepared',len(landmarks),'landmarks and',len(jobs),'fictional deliveries.')
