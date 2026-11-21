import struct, numpy as np
def load(path):
    b=open(path,'rb').read(); p=[0]
    def rd(fmt):
        s=struct.calcsize(fmt); v=struct.unpack_from(fmt,b,p[0]); p[0]+=s; return v
    def u32(): return rd('<I')[0]
    def st():
        l=b[p[0]]; p[0]+=1; s=b[p[0]:p[0]+l].decode(); p[0]+=l; return s
    assert b[:4]==b'SKHM'; p[0]=4; u32(); nV=u32()
    base=np.frombuffer(b,'<f4',nV*3,p[0]).reshape(nV,3).copy(); p[0]+=nV*12
    no=u32(); oo=np.frombuffer(b,np.dtype([('v','<u4'),('u','<f4'),('t','<f4')]),no,p[0]); p[0]+=no*12
    oOrig=oo['v'].copy(); uv=np.stack([oo['u'],oo['t']],1).copy()
    ni=u32(); idx=np.frombuffer(b,'<u4',ni,p[0]).copy(); p[0]+=ni*4
    nb=u32(); bones=[]
    for _ in range(nb):
        n=st(); par=rd('<h')[0]; c=u32(); h=list(rd('<%dI'%c)); c=u32(); t=list(rd('<%dI'%c)); bones.append(dict(name=n,parent=par,head=h,tail=t))
    wj=np.zeros((13380,4),np.uint8); ww=np.zeros((13380,4),np.uint8)
    raw=np.frombuffer(b,np.uint8,13380*8,p[0]).reshape(13380,8); p[0]+=13380*8
    wj=raw[:,:4].copy(); ww=raw[:,4:].copy()
    nt=u32(); T={}
    for _ in range(nt):
        n=st(); c=u32(); sc=rd('<f')[0]
        arr=np.frombuffer(b,np.dtype([('i','<u2'),('d','<i2',3)]),c,p[0]); p[0]+=c*8
        T[n]=(arr['i'].astype(int),arr['d'].astype(np.float32)*(sc/32767.0))
    return dict(nV=nV,base=base,oOrig=oOrig,uv=uv,idx=idx,bones=bones,wj=wj,ww=ww,T=T)
