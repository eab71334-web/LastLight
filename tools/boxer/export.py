import numpy as np, struct, sys, os
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from mesh import *; from anims import *; from bake import *
def main(out):
    d=make_body(); r=Rig(d); m=build(d,r); res=bake_all(r)
    b=bytearray()
    def S(s): e=s.encode(); b.append(len(e)); b.extend(e)
    def I(*a):
        for x in a: b.extend(struct.pack('<i',int(x)))
    def F(arr): b.extend(np.asarray(arr,'<f4').tobytes())
    b.extend(b'BXR1')
    I(r.n)
    for i in range(r.n):
        S(r.name[i]); I(r.par[i]); p=r.rest[i]; F([-p[0],p[1],p[2]])
    nv=len(m['V']); I(nv)
    V=m['V'].copy(); V[:,0]*=-1; N=m['N'].copy(); N[:,0]*=-1
    F(V); F(N); F(m['UV'])
    b.extend(m['J'].astype(np.uint8).tobytes())
    W=np.round(m['W']*255).astype(int)
    for k in range(nv):   # make each row sum exactly 255
        s=W[k].sum()
        if s!=255: W[k,int(np.argmax(W[k]))]+=255-s
    b.extend(W.astype(np.uint8).tobytes())
    I(len(m['subs']))
    for role,T in m['subs']:
        S(role); I(len(T)*3); t=T[:,[0,2,1]].astype('<u2'); b.extend(t.tobytes())
    I(len(res))
    for name,v in res.items():
        S(name); F([v['T']]); b.append(1 if v['loop'] else 0); I(len(v['Q']))
        R=v['R'].copy(); R[:,0]*=-1; F(R)
        Q=v['Q'].copy(); Q[:,:,1]*=-1; Q[:,:,2]*=-1; F(Q)
    os.makedirs(os.path.dirname(out),exist_ok=True); open(out,'wb').write(b)
    print('wrote',out,len(b),'bytes; verts',nv,'bones',r.n,'clips',list(res))
if __name__=='__main__': main(sys.argv[1])
