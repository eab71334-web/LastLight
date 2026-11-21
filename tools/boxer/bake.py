import numpy as np, sys
sys.path.insert(0,'.')
from mesh import *; from anims import *
def bake_all(rig):
    res={}
    for name,(T,loop,keys) in CLIPS.items():
        n=int(round(T*FPS)); frames=n if loop else n+1
        Q=[];R=[]
        for f in range(frames):
            q,off=rig.solve(sample(keys,f/FPS)); 
            if Q:
                d=(q*Q[-1]).sum(1); q=q*np.where(d<0,-1,1)[:,None]
            Q.append(q);R.append(off)
        res[name]=dict(T=T,loop=loop,Q=np.array(Q),R=np.array(R))
    return res
