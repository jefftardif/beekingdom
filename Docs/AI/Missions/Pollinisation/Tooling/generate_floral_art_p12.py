# BeeKingdom floral-site sprite generator. New assets only, no terrain edits.
from PIL import Image, ImageDraw, ImageFilter
import math, random, pathlib
BASE = pathlib.Path(__file__).resolve().parents[5]
OUT = BASE / "Assets/BeeKingdom/Playground/Resources/Pollination"
OUT.mkdir(parents=True,exist_ok=True)
S=3
def p(x): return round(x*S)
def ell(d,box,fill,outline=None,width=1):
    d.ellipse(tuple(p(v) for v in box),fill=fill,outline=outline,width=p(width))
def poly(d,pts,c): d.polygon([(p(x),p(y)) for x,y in pts],fill=c)
def line(d,pts,c,w=1): d.line([(p(x),p(y)) for x,y in pts],fill=c,width=p(w),joint="curve")
def petal(im,cx,cy,ang,size,palette):
    w,h=size; pet=Image.new("RGBA",(p(w*2.5),p(h*2.5)),(0,0,0,0))
    d=ImageDraw.Draw(pet)
    mid=p(w*1.25); bottom=p(h*1.85)
    d.ellipse((mid-p(w*.48),bottom-p(h*1.37),mid+p(w*.48),bottom+p(h*.06)),fill=palette[0])
    d.ellipse((mid-p(w*.32),bottom-p(h*1.21),mid+p(w*.31),bottom-p(h*.12)),fill=palette[1])
    d.ellipse((mid-p(w*.20),bottom-p(h*1.20),mid+p(w*.15),bottom-p(h*.52)),fill=palette[2])
    rotated=pet.rotate(ang,Image.Resampling.BICUBIC,expand=True)
    im.alpha_composite(rotated,(p(cx)-rotated.width//2,p(cy)-rotated.height//2))
def leaf(im,cx,cy,ang,scale=1):
    w,h=18*scale,32*scale
    l=Image.new("RGBA",(p(60*scale),p(65*scale)),(0,0,0,0));d=ImageDraw.Draw(l)
    dx=p(30*scale);dy=p(31*scale)
    d.ellipse((dx-p(w*.45),dy-p(h*.50),dx+p(w*.48),dy+p(h*.50)),fill=(36,89,55,238))
    d.ellipse((dx-p(w*.34),dy-p(h*.44),dx+p(w*.25),dy+p(h*.36)),fill=(76,148,86,230))
    d.line((dx,dy+p(h*.39),dx,dy-p(h*.30)),fill=(179,199,105,135),width=p(1))
    r=l.rotate(ang,Image.Resampling.BICUBIC,expand=True)
    im.alpha_composite(r,(p(cx)-r.width//2,p(cy)-r.height//2))
def flower(im,x,y,r,kind,rng,scale=1):
    d=ImageDraw.Draw(im)
    if kind=="gold":
        palette=((224,133,28,240),(249,203,82,250),(255,237,163,238))
        num=6
    elif kind=="lilac":
        palette=((99,69,154,245),(184,148,219,252),(229,211,247,245))
        num=5
    elif kind=="white":
        palette=((165,173,165,239),(247,238,218,250),(255,255,246,252))
        num=6
    else:
        palette=((177,67,90,245),(240,148,161,255),(255,209,196,245))
        num=5
    # soft under-shadow; each flower drawn on full patch canvas
    ell(d,(x-r*.9,y-r*.45,x+r*.95,y+r*.85),(9,38,25,85))
    for i in range(num):
        angle=360*i/num+rng.uniform(-8,8)
        theta=math.radians(angle)
        petal(im,x+math.cos(theta)*r*.40,y+math.sin(theta)*r*.40,angle-90,(r*.92,r*1.28),palette)
    d=ImageDraw.Draw(im)
    ell(d,(x-r*.24,y-r*.24,x+r*.24,y+r*.24),(120,72,28,248))
    ell(d,(x-r*.17,y-r*.17,x+r*.17,y+r*.17),(244,182,48,255))
    for i in range(6):
        a=2*math.pi*i/6
        qx,qy=x+math.cos(a)*r*.095,y+math.sin(a)*r*.10
        ell(d,(qx-0.9,qy-0.9,qx+0.9,qy+0.9),(252,247,175,220))
def build(name,mode,seed):
    rng=random.Random(seed)
    W,H=332,238
    im=Image.new("RGBA",(p(W),p(H)),(0,0,0,0))
    d=ImageDraw.Draw(im)
    # grounded meadow: loose foliage instead of a generic icon circle
    for i in range(110 if mode=="bloom" else 70):
        t=rng.random()*math.tau
        rad=rng.random()**.5
        x=W/2+math.cos(t)*rad*140
        y=H*.54+math.sin(t)*rad*72
        size=rng.uniform(2.5,9)
        shade=rng.choice([(24,72,44,140),(48,94,52,177),(85,133,70,120),(111,145,81,155)])
        ell(d,(x-size,y-size*.25,x+size,y+size*.48),shade)
    density={"bloom":17,"recovering":10,"resting":5}[mode]
    flowers=[]
    for i in range(density):
        a=rng.random()*math.tau
        r=math.sqrt(rng.random())
        x=W/2+math.cos(a)*r*118
        y=H*.49+math.sin(a)*r*61
        flowers.append((y,x,rng.uniform(11,16),rng.choice(["gold","white","lilac","rose"])))
    flowers.sort()
    for y,x,r,k in flowers:
        for i in range(rng.randint(3,5)):
            dx,dy=rng.uniform(-22,22),rng.uniform(-24,24)
            leaf(im,x+dx,y+dy,rng.uniform(-180,180),rng.uniform(.55,1.08))
        flower(im,x,y,r,k,rng)
    if mode!="bloom":
        d=ImageDraw.Draw(im)
        for i in range(7 if mode=="recovering" else 13):
            x=rng.uniform(38,296); y=rng.uniform(68,189)
            line(d,[(x,y+12),(x+rng.uniform(-5,5),y)],(49,105,59,185),1.8)
            ell(d,(x-3.1,y-4,x+3.1,y+3),(112,151,75,220) if mode=="recovering" else (105,91,63,187))
    # finish: downsample from supersampled painterly primitives
    im=im.resize((W,H),Image.Resampling.LANCZOS)
    im.save(OUT/name,optimize=True)
def bee():
    W,H=92,80
    im=Image.new("RGBA",(p(W),p(H)),(0,0,0,0));d=ImageDraw.Draw(im)
    # transparent wings with luminous edging
    ell(d,(21,8,48,44),(185,229,241,112),(227,246,247,167),1)
    ell(d,(45,9,73,44),(185,229,241,112),(227,246,247,167),1)
    ell(d,(28,28,65,67),(74,45,23,228))
    ell(d,(32,28,62,65),(241,178,59,252))
    for x in (37,47,57):
        line(d,[(x,34),(x+2,48),(x,57)],(61,42,29,224),3.2)
    ell(d,(36,23,57,39),(94,65,35,250))
    ell(d,(40,26,44,30),(247,235,169,255))
    for x in (38,52):
        line(d,[(x,28),(x-3,18)],(77,51,35,205),1.3)
    for x in (35,61):
        line(d,[(x,52),(x+(-9 if x<50 else 9),65)],(72,53,39,189),1.6)
    im.resize((W,H),Image.Resampling.LANCZOS).save(OUT/"forager.png",optimize=True)
for name,mode,seed in [("field_bloom.png","bloom",401),("field_recovering.png","recovering",403),("field_resting.png","resting",406)]:
    build(name,mode,seed)
bee()
for f in OUT.glob("*.png"): print(f.name, f.stat().st_size)
