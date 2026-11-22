"""First-person arm + glove (left & right) cut from the same human mesh, in a local frame:
origin = wrist, +z = along forearm to fist, +y = up, +x = Y x z.  Binary 'FPA1' (right-handed numbers)."""
import numpy as np, struct, sys, os
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from mesh import *
def main(out):
    d=make_body(); r=Rig(d); m=build(d,r); V=m['V']; N=m['N']
    sub=dict(m['subs']); b=bytearray(b'FPA1')
    from rig import PALM_SIGN
    q0,off0=r.solve({'curl':1.0}); Vp=skin(m,r,q0,off0); Np=normals(Vp,np.concatenate([t for _,t in m['subs']]))
    for s in 'LR':
        wi=r.ix[f'wrist.{s}']; w0=r.rest[wi]; dr=r.rd['la'+s]
        z=dr; x=unit(np.cross([0,1,0],z)); y=np.cross(z,x)
        Rm=np.stack([x,y,z])          # rows = basis
        def sel(T,mask):
            tri=T[mask]; used=np.unique(tri); rm=-np.ones(len(V),int); rm[used]=np.arange(len(used)); return used,rm[tri]
        Ts=sub['skin']; cs=V[Ts].mean(1); rel=cs-w0; pr=rel@dr; lat=np.linalg.norm(rel-np.outer(pr,dr),axis=1)
        sk=(pr>-0.60)&(pr<0.35)&(lat<0.16)
        parts=[]
        for T,mask in ((Ts,sk),(sub['tape'],(np.linalg.norm(V[sub['tape']].mean(1)-w0,axis=1)<0.3))):
            parts.append(sel(T,mask))
        U=np.concatenate([parts[0][0],parts[1][0]]); off=len(parts[0][0])
        P=(Vp[U]-w0)@Rm.T; Nn=Np[U]@Rm.T
        # bake roll: palm faces inward (local -x for L, +x for R) so the viewer needs no extra roll
        nl=Rm@r.palm[s]; cur=np.arctan2(nl[1],nl[0]); tgt=np.pi if s=='L' else 0.0; phi=tgt-cur
        c,sn=np.cos(phi),np.sin(phi); Rz=np.array([[c,-sn,0],[sn,c,0],[0,0,1]])
        P=P@Rz.T; Nn=Nn@Rz.T
        b.extend(struct.pack('<i',len(U))); b.extend(np.asarray(P,'<f4').tobytes()); b.extend(np.asarray(Nn,'<f4').tobytes())
        for k,(u,tri) in enumerate(parts):
            tri=tri+(off if k else 0); b.extend(struct.pack('<i',len(tri))); b.extend(np.asarray(tri.reshape(-1),'<i4').tobytes())
        print(s,len(U),[len(p[1]) for p in parts],'z range',P[:,2].min(),P[:,2].max())
    open(out,'wb').write(b)
if __name__=='__main__': main(sys.argv[1])
