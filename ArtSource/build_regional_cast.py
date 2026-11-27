"""Bake each destination's costumes and food into an independently loadable CCM3 library."""
import math
import struct
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "Assets/ChronosChef/Resources"
source = (OUT / "KitchenModels.bytes").read_bytes()
pivot_end = 5 + source[4] * 12
cursor = pivot_end + 4
parts = {}
while cursor < len(source):
    length = source[cursor]
    name = source[cursor + 1:cursor + 1 + length].decode()
    cursor += 1 + length
    count, = struct.unpack_from("<i", source, cursor)
    cursor += 4
    parts[name] = source[cursor:cursor + count * 76]
    cursor += count * 76

# Costume cloth, metal/trim, garnish and soup for each region.
palettes = [
    ("427348","e4b444","e24430","ed9136"), ("784128","d09850","d59134","a85120"),
    ("2b6580","f0ddab","ed7758","e6bc82"), ("924144","eeeecc","d54659","f1ce87"),
    ("59833c","d79b53","7fc344","a4672c"), ("32747a","7fe3ec","53cffa","60acbb"),
    ("704089","83e5bc","cf72f2","796ad4"),
]
def rgb(code):
    return bytes.fromhex(code)

def ellipsoid(center, size, tint, joint=0, rings=5, sides=12, tip=0):
    result = bytearray()
    def point(row,col):
        phi = math.pi * row / rings - math.pi / 2
        theta = math.tau * col / sides
        u,v,w = math.cos(phi)*math.cos(theta),math.cos(phi)*math.sin(theta),math.sin(phi)
        n = (u/size[0],v/size[1],w/size[2])
        length = math.sqrt(sum(k*k for k in n))
        return struct.pack("<6f",center[0]+u*size[0]+tip*w,center[1]+v*size[1],center[2]+w*size[2],*(k/length for k in n))
    for row in range(rings):
        for col in range(sides):
            a,b,c,d = point(row,col),point(row,col+1),point(row+1,col+1),point(row+1,col)
            if row: result.extend(rgb(tint)+bytes([joint])+a+b+c)
            if row+1<rings: result.extend(rgb(tint)+bytes([joint])+a+c+d)
    return result

def costume(world, kind, cloth, trim):
    z=(2.57,2.39,2.48,2.34)[kind]
    def piece(x,y,d,s,t,c=cloth,tip=0):
        return ellipsoid((x,y,z+d),s,c,1,5,t,tip)
    cap=piece(0,.01,.03,(.57,.43,.14),16)
    if world==0: # orchard straw hats with a berry pin
        return cap+piece(0,-.02,-.04,(.75,.58,.045),20,trim)+piece(.38,-.29,.09,(.1,.09,.11),10,"c24e39")
    if world==1: # lamp, brass casing and lens
        return cap+piece(0,-.39,.01,(.2,.11,.19),14,trim)+piece(0,-.49,.01,(.14,.055,.13),14,"ffedac")
    if world==2: # peaked sailor cap with turned up crown
        return cap+piece(0,-.34,-.06,(.48,.29,.055),16)+piece(0,.02,.16,(.62,.44,.14),16,trim)
    if world==3: # alpine wool and pompom
        return cap+piece(0,.02,.14,(.48,.39,.24),16)+piece(0,.02,.39,(.17,.16,.15),12,trim)
    if world==4:
        return cap+b''.join(piece(x,.02,.18,(.105,.10,.32-abs(x)*.22),8,trim,x*.5) for x in (-.37,0,.37))
    if world==5:
        return cap+b''.join(piece(s*.55,0,-.15,(.12,.24,.24),12,trim) for s in (-1,1))+piece(0,-.38,0,(.4,.06,.05),12,"a1fff5")
    return cap+b''.join(piece(s*.39,.04,.23,(.16,.11,.32),10,trim,s*.14)+piece(s*.52,.04,.51,(.16,.14,.11),12,"e7a8f5") for s in (-1,1))

def recolor(records, mapping):
    result=bytearray(records)
    for i in range(0,len(result),76):
        tint=mapping.get(bytes(result[i:i+3]))
        if tint: result[i:i+3]=tint
    return result

for world,(cloth,trim,fruit,broth) in enumerate(palettes):
    models={}
    for kind,name in enumerate(("Grog","BipIdle","Zorg","Rex","BipAlarm")):
        k=1 if kind==4 else kind
        mapping={rgb(c):rgb(cloth) for c in ("a65738","8957a6","344768","bdb2c3","8c628b")}
        models[name]=recolor(parts[name],mapping)+costume(world,k,cloth,trim)
    for dish in ("Burger","Soup","Toast"):
        data=bytearray(parts[dish])
        # Fit the toppings to the actual crown, liquid surface or slice.
        points=[struct.unpack_from("<3f",data,i+4+j*24) for i in range(0,len(data),76) for j in range(3)]
        top=max(z for x,y,z in points)
        if dish=="Soup":
            # Tint the liquid and bowl independently while preserving highlights.
            for i in range(0,len(data),76):
                c=data[i:i+3]
                if c[1]>c[0]*1.12: data[i:i+3]=rgb(broth)
            top-=.07
        for j in range(3+world%3):
            a=math.tau*j/(3+world%3)
            x,y=.34*math.cos(a),.24*math.sin(a)
            data.extend(ellipsoid((x,y,top+.01),(.13,.08,.045+world%3*.03),fruit,0,4,9))
            data.extend(ellipsoid((x+.07,y+.02,top+.07),(.095,.042,.025),"779947",0,3,8))
        models[dish]=data
    out=bytearray(source[:pivot_end]+struct.pack("<i",len(models)))
    for name,data in models.items():
        key=("W"+str(world)+name).encode()
        out.extend(bytes([len(key)])+key+struct.pack("<i",len(data)//76)+data)
    (OUT/f"ServiceCast{world}.bytes").write_bytes(out)
