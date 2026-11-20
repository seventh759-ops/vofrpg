from common_body import *
def warden(kind):
    """灯番の系統。kind: base / sun / dawn / bastion / aegis"""
    c=C(96,136,10)
    shield = kind in ('bastion','aegis')
    holy = kind in ('sun','dawn')
    # 光の翼（夜明けの者）：背中に光る羽
    if kind=='dawn':
        for side in (-1,1):
            for k in range(3):
                cx=46+side*(24+k*6); cy=30+k*10
                c.ell(cx,cy,5,10,'l'); c.ell(cx,cy+1,2.5,6,'o')
    # 脚・すね当て・靴
    c.poly([(36,84),(45,84),(44,108),(35,108)],'m'); c.poly([(48,84),(57,84),(58,108),(49,108)],'m')
    c.rect(36,92,44,94,'g'); c.rect(49,92,57,94,'g')
    boots(c,106)
    # 胴：鎖かたびら
    c.poly([(35,32),(57,32),(60,84),(32,84)],'m')
    # 陣羽織と金の十字
    c.poly([(36,44),(56,44),(62,92),(30,92)],'v')
    c.rect(44,52,48,82,'g'); c.rect(38,60,54,64,'g')
    c.poly([(30,88),(62,88),(62,92),(30,92)],'g')
    for (x0,y0,x1,y1) in [(36,66,34,88),(56,66,58,88)]: c.line(x0,y0,x1,y1,'f' if kind=='base' else 'F',1,only='v')
    # 帯
    c.poly([(34,52),(58,52),(58,56),(34,56)],'b'); c.rect(43,51,49,57,'g')
    # 肩当て（白い大きな）
    for sx in (32,60):
        c.ell(sx,35,9,6,'x'); c.line(sx-8,38,sx+8,38,'g',1.5)
    # 腕
    c.poly([(26,38),(33,38),(32,64),(25,64)],'m'); c.rect(24,60,33,64,'x'); hand(c,29,68)
    c.poly([(59,38),(66,38),(67,64),(60,64)],'m'); c.rect(59,60,68,64,'x'); hand(c,64,68)
    # 腰のランタン（右）
    c.line(66,70,70,78,'m'); c.rect(66,78,76,92,'m'); c.rect(68,80,74,90,'l'); c.ell(71,85,1.8,3,'L')
    c.rect(65,92,77,94,'m')
    # 盾（左手）
    if shield:
        big = kind=='aegis'
        x0,x1,y0,y1 = (6,34,46,104) if big else (10,32,50,98)
        c.poly([(x0,y0),(x1,y0),(x1,y1-8),((x0+x1)/2,y1),(x0,y1-8)],'u')
        c.poly([(x0,y0),(x1,y0),(x1,y1-8),((x0+x1)/2,y1),(x0,y1-8)],'g',only='.')
        c.poly([(x0+2,y0+2),(x1-2,y0+2),(x1-2,y1-9),((x0+x1)/2,y1-2),(x0+2,y1-9)],'u')
        mx=(x0+x1)/2; c.rect(mx-1.5,y0+8,mx+1.5,y1-14,'x'); c.rect(mx-7,y0+16,mx+7,y0+19,'x')
    neck(c,'m')
    # 頭：兜と鎖の頭巾
    c.ell(46,15,11,12,'m')
    c.ell(46,17,7,8,'s')
    for ex in (42,49):
        c.rect(ex,16,ex+1,18,'B' if kind=='aegis' else 'e')
        if kind!='aegis': c.set(ex,16,'x')
        c.line(ex-1,14,ex+2,14,'h')
    c.poly([(40,21),(52,21),(51,25),(41,25)],'m')         # 口元を覆う鎖
    c.poly([(35,5),(57,5),(56,10),(36,10)],'m'); c.rect(35,9,57,11,'g')  # 兜のふち
    c.ell(46,5,10,5,'m',only='.')
    if kind=='base':
        c.poly([(45,-1),(48,-1),(50,-8),(46,-12),(44,-6)],'r'); c.poly([(48,-4),(54,-10),(52,-3)],'f')
    if holy:
        for k in range(9):
            a=math.radians(200+k*17.5); c.ell(46+15*math.cos(a),2+5*math.sin(a),1.2,1.2,'o')
        c.poly([(43,0),(49,0),(46,-6)],'g')
    if shield:
        c.poly([(36,4),(30,-6),(34,-7),(39,2)],'w'); c.poly([(56,4),(62,-6),(58,-7),(53,2)],'w')
        c.ell(30,-7,1.5,1.5,'g'); c.ell(62,-7,1.5,1.5,'g')
    if kind=='aegis':
        for (x,y) in [(84,40),(86,100),(10,24)]: sparkle(c,x,y,'o',2)
    return c
