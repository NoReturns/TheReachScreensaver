// R17 isolated developer fixture. The surrounding bundle supplies Q (engine), X
// (fixture contract), Z (display), zm (physical size), nh/rh (black hole).
const r17={phase:'idle',frames:[],errors:[],intervals:[],last:null,callbacks:0,
  captured:0,overflow:0,contextLost:false,epoch:0,duration:60000};
window.__r17Start=(epoch,capture)=>{
  if(!X||X.mode!=='cylindrical'||X.post!=='final'||X.guardPhysicalPixels!==512
    ||r17.phase!=='idle'||epoch<Date.now()+300)throw Error('Invalid R17 start');
  const gl=Q.renderer.getContext();
  const scenario=X.fixtureId==='galaxy-sector'?'planet':
    X.fixtureId==='galaxy-left-seam'?'stars':
    X.fixtureId==='hole-26-left'?'hole':null;
  if(!scenario)throw Error('Invalid R17 scenario');
  const base=scenario==='planet'?23:scenario==='stars'?-78:0;
  const crop=Z==='left'?(scenario==='planet'
    ?{x:2350,y:500,width:1200,height:720}
    :scenario==='stars'?{x:2500,y:500,width:1200,height:720}
    :{x:zm.width-320,y:700,width:320,height:700})
    :Z==='primary'?{x:0,y:700,width:320,height:700}:null;
  if(crop&&(crop.x+crop.width>zm.width||crop.y+crop.height>zm.height))
    throw Error('R17 crop exceeds physical output');
  const canvas=document.querySelector('#scene');
  canvas.addEventListener('webglcontextlost',()=>{r17.contextLost=true},{once:true});
  r17.phase='waiting';r17.epoch=epoch;r17.scenario=scenario;
  r17.crop=crop;r17.capture=Boolean(capture);
  const tick=t=>{
    if(r17.phase==='failed'||r17.phase==='done')return;
    const callbackMs=performance.timeOrigin+t;
    if(callbackMs<epoch){requestAnimationFrame(tick);return;}
    const elapsed=Math.min(r17.duration,callbackMs-epoch);
    const second=Math.min(60,Math.floor(elapsed/1000));
    // Every compared sample is rendered at the same integer-second state.
    // Between samples the camera follows a smooth, continuously paced path.
    const sample=r17.capture&&crop&&second>0&&second<60
      &&second!==r17.lastSample;
    const virtualMs=sample?second*1000:elapsed;
    const yaw=scenario==='planet'?base+16*(virtualMs/60000)
      :scenario==='stars'?base+0.4*(virtualMs/60000)
      :base+8*Math.sin(2*Math.PI*virtualMs/20000);
    try{
      if(r17.last!==null)r17.intervals.push(callbackMs-r17.last);
      r17.last=callbackMs;r17.callbacks++;
      Q.camera.quaternion.setFromAxisAngle(new B(0,1,0),yaw*Math.PI/180);
      Q.camera.updateMatrixWorld();
      if(scenario==='hole')Q.blackHole.target(nh,rh,Q.camera.quaternion,Q.camera,
        Q.localVerticalFovRad,Q.height,0);
      Q.render();Zm++;
      if(sample){
        r17.lastSample=second;
        const pixels=new Uint8Array(crop.width*crop.height*4);
        const readStart=performance.timeOrigin+performance.now();
        gl.readPixels(crop.x,zm.height-crop.y-crop.height,crop.width,crop.height,
          gl.RGBA,gl.UNSIGNED_BYTE,pixels);
        const readEnd=performance.timeOrigin+performance.now();
        if(gl.getError()!==gl.NO_ERROR)throw Error('R17 readPixels failed');
        if(r17.frames.length>=64)r17.overflow++;
        else r17.frames.push({second,virtualMs,yaw,callbackMs,readStart,readEnd,
          crop,base64:_h(pixels)});
        r17.captured++;
      }
    }catch(error){r17.errors.push(String(error));r17.phase='failed';return;}
    if(elapsed>=r17.duration){r17.phase='done';return;}
    requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
  return {scenario,epoch,crop,capture};
};
window.__r17Drain=()=>({phase:r17.phase,scenario:r17.scenario,
  callbacks:r17.callbacks,captured:r17.captured,queued:r17.frames.length,
  overflow:r17.overflow,errors:r17.errors,contextLost:r17.contextLost,
  crop:r17.crop,intervals:r17.phase==='done'&&r17.frames.length===0?r17.intervals:null,
  frame:r17.frames.shift()??null});
