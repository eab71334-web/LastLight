import numpy as np, matplotlib; matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection
from mesh import *
COL={'skin':(0.78,0.58,0.45),'shorts':(0.1,0.25,0.75),'band':(0.95,0.95,0.95),'hair':(0.08,0.06,0.05),'glove':(0.8,0.08,0.08),'tape':(0.93,0.93,0.9),'eyew':(1,1,1),'iris':(0.1,0.05,0.02)}
def draw(ax,m,Vs,yaw,xi=0):
    R=Ry(yaw); V=Vs@R.T
    polys=[];cols=[];dep=[]
    L=unit(np.array([0.3,0.6,0.8]))
    for role,T in m['subs']:
        a=V[T[:,0]];b=V[T[:,1]];c=V[T[:,2]]; fn=np.cross(b-a,c-a); fn/= (np.linalg.norm(fn,axis=1,keepdims=True)+1e-12)
        vis=fn[:,2]>-0.02   # facing camera (camera at +z looking -z)
        sh=np.clip(fn@L,0,1)*0.65+0.35
        base=np.array(COL[role])
        for k in np.where(vis)[0]:
            polys.append(np.stack([[a[k,0],a[k,1]],[b[k,0],b[k,1]],[c[k,0],c[k,1]]])); cols.append(np.clip(base*sh[k],0,1)); dep.append((a[k,2]+b[k,2]+c[k,2])/3)
    o=np.argsort(dep)
    ax.add_collection(PolyCollection([polys[i] for i in o],facecolors=[cols[i] for i in o],edgecolors=[cols[i] for i in o],linewidths=0.4))
def render(m,rig,q,rootpos,path,views=(0,90,35),title=''):
    Vs=skin(m,rig,q,rootpos)
    fig,axs=plt.subplots(1,len(views),figsize=(4.2*len(views),5.4),dpi=80)
    for ax,yv in zip(np.atleast_1d(axs),views):
        # yaw: view from front means looking along -z (camera at +z): yv rotates the model
        draw(ax,m,Vs,yv); ax.set_xlim(-1.0,1.0); ax.set_ylim(-0.05,2.0); ax.set_aspect('equal'); ax.axhline(0,color='k',lw=0.5); ax.set_title(f'{title} view {yv}',fontsize=8); ax.axis('off')
    fig.tight_layout(); fig.savefig(path); plt.close(fig)

def sheet(m,rig,items,path,views=(-30,90),cell=(2.6,4.2)):
    """items: list of (title,pose)"""
    from poses import P
    fig,axs=plt.subplots(len(views),len(items),figsize=(cell[0]*len(items),cell[1]*len(views)),dpi=70)
    axs=np.array(axs).reshape(len(views),len(items))
    for c,(t,pose) in enumerate(items):
        q,off=rig.solve(pose); Vs=skin(m,rig,q,off)
        for r_,yv in enumerate(views):
            ax=axs[r_,c]; draw(ax,m,Vs,yv); ax.set_xlim(-0.9,0.9); ax.set_ylim(-0.05,1.95); ax.set_aspect('equal'); ax.axhline(0,color='k',lw=0.5); ax.axis('off')
            if r_==0: ax.set_title(t,fontsize=9)
    fig.tight_layout(); fig.savefig(path); plt.close(fig)
