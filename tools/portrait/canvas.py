import math
class C:
    def __init__(s,w,h,oy=0): s.w,s.h,s.oy=w,h,oy; s.g=[['.']*w for _ in range(h)]  # oy：描く座標を下へずらす（帽子・光輪が上にはみ出さないように）
    def set(s,x,y,c):
        x,y=int(round(x)),int(round(y))+s.oy
        if 0<=x<s.w and 0<=y<s.h: s.g[y][x]=c
    def get(s,x,y):
        y=y+s.oy
        return s.g[y][x] if 0<=x<s.w and 0<=y<s.h else '.'
    def ell(s,cx,cy,rx,ry,c,only=None):
        for y in range(int(cy-ry)-1,int(cy+ry)+2):
            for x in range(int(cx-rx)-1,int(cx+rx)+2):
                if ((x-cx)/rx)**2+((y-cy)/ry)**2<=1:
                    if only is None or s.get(x,y) in only: s.set(x,y,c)
    def poly(s,pts,c,only=None):
        ys=[p[1] for p in pts]
        for y in range(int(min(ys)),int(max(ys))+1):
            yy=y+0.5; xs=[]
            for i in range(len(pts)):
                (x1,y1),(x2,y2)=pts[i],pts[(i+1)%len(pts)]
                if (y1<=yy<y2) or (y2<=yy<y1): xs.append(x1+(yy-y1)*(x2-x1)/(y2-y1))
            xs.sort()
            for a,b in zip(xs[::2],xs[1::2]):
                for x in range(int(math.ceil(a-0.5)),int(math.floor(b-0.5))+1):
                    if only is None or s.get(x,y) in only: s.set(x,y,c)
    def rect(s,x0,y0,x1,y1,c,only=None):
        x0,y0,x1,y1=int(round(x0)),int(round(y0)),int(round(x1)),int(round(y1))
        for y in range(y0,y1+1):
            for x in range(x0,x1+1):
                if only is None or s.get(x,y) in only: s.set(x,y,c)
    def line(s,x0,y0,x1,y1,c,t=1,only=None):
        n=int(max(abs(x1-x0),abs(y1-y0))*2)+1
        for i in range(n+1):
            x=x0+(x1-x0)*i/n; y=y0+(y1-y0)*i/n
            if t<=1: 
                if only is None or s.get(int(round(x)),int(round(y))) in only: s.set(x,y,c)
            else: s.ell(x,y,t/2,t/2,c,only)
    def rows(s): return [''.join(r) for r in s.g]
def cs(name,rows):
    out=[f'            ["{name}"] = new[]','            {']
    out+= [f'                "{r}",' for r in rows]
    out+= ['            },']
    return '\n'.join(out)
