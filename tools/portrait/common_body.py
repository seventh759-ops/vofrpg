from canvas import *
# 6頭身の共通の骨組み：頭の中心 (46,16)、顔 rx7.5 ry9、肩 y≈30、帯 y≈58、裾 y≈108、足 y≈122
HX,HY=46,16
def face(c,eye='e',hair='h',bangs=True,hl='x'):
    c.ell(HX,HY,7.5,9,'s')
    if bangs:
        c.poly([(38,9),(54,9),(55,15),(52,12),(49,15),(46,11),(43,15),(40,12),(37,16)],hair)
        c.poly([(38,10),(40,10),(39,22),(37,20)],hair); c.poly([(52,10),(55,10),(55,20),(53,22)],hair)
    for ex in (42,49):
        c.rect(ex,16,ex+1,18,eye)
        if hl and eye=='e': c.set(ex,16,hl)
        c.line(ex-1,14,ex+2,14,hair if hair!='x' else 'q')
def neck(c,col='s'):
    c.rect(44,24,48,29,col)
def hand(c,x,y): c.ell(x,y,3.5,3.8,'s')
def boots(c,y0=112,col='b'):
    c.poly([(34,y0),(43,y0),(44,122),(32,122)],col)
    c.poly([(49,y0),(58,y0),(60,122),(48,122)],col)
SPARK={'o':'1','p':'2','B':'3','W':'4','L':'5','R':'6'}
def sparkle(c,x,y,col='o',r=2):
    """宙に浮かぶきらめき（輪郭なしの光。数字の文字で描く）。"""
    k=SPARK.get(col,col)
    c.line(x-r,y,x+r,y,k); c.line(x,y-r,x,y+r,k)
