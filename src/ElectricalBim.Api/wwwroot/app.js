const projectId='demo';
const $=id=>document.getElementById(id);
async function api(path,options){const response=await fetch(path,options);if(response.status===204)return null;if(!response.ok)throw new Error(await response.text());return response.json()}
async function refresh(){
  try{
    const [elements,jobs]=await Promise.all([api(`/api/projects/${projectId}/elements`),api(`/api/projects/${projectId}/jobs`)]);
    $('health').textContent='API online';$('health').classList.add('online');$('elementCount').textContent=elements.length;$('projectLabel').textContent=projectId;
    const groups=Object.entries(elements.reduce((a,e)=>(a[e.category]=(a[e.category]||0)+1,a),{})).sort((a,b)=>b[1]-a[1]);$('categoryCount').textContent=groups.length;
    const max=Math.max(1,...groups.map(x=>x[1]));$('categories').innerHTML=groups.length?groups.map(([name,count])=>`<div class="bar"><span>${escapeHtml(name)}</span><div class="track"><div class="fill" style="width:${count/max*100}%"></div></div><strong>${count}</strong></div>`).join(''):'<p class="muted">Waiting for Revit sync…</p>';
    $('jobCount').textContent=jobs.filter(x=>x.status===0).length;$('jobs').innerHTML=jobs.slice(0,6).map(j=>`<div class="job"><strong>${escapeHtml(j.type)}</strong><br>${escapeHtml(j.agentId)} · ${['Queued','Running','Completed','Failed'][j.status]}</div>`).join('');
  }catch(e){$('health').textContent='API offline';$('health').classList.remove('online')}
}
function escapeHtml(value){const d=document.createElement('div');d.textContent=value??'';return d.innerHTML}
$('refresh').onclick=refresh;
$('chatForm').onsubmit=async e=>{e.preventDefault();const message=$('chatInput').value;$('chat').insertAdjacentHTML('beforeend',`<p class="user">${escapeHtml(message)}</p>`);$('chatInput').value='';try{const r=await api(`/api/projects/${projectId}/chat`,{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({message})});$('chat').insertAdjacentHTML('beforeend',`<p class="assistant">${escapeHtml(r.answer)}</p>`)}catch{$('chat').insertAdjacentHTML('beforeend','<p class="assistant">Server tidak tersedia.</p>')}};
$('jobForm').onsubmit=async e=>{e.preventDefault();await api(`/api/projects/${projectId}/jobs`,{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({agentId:$('agentId').value,type:$('jobType').value,payload:{}})});await refresh()};
refresh();setInterval(refresh,3000);
