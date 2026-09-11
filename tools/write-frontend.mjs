import fs from 'node:fs';
const files=JSON.parse(fs.readFileSync(process.argv[2],'utf8'));
for(const [path,content] of Object.entries(files)) {
 if(!path.startsWith('frontend/mapan-web/src/'))throw Error('Path outside frontend source');
 fs.mkdirSync(path.slice(0,path.lastIndexOf('/')),{recursive:true});fs.writeFileSync(path,content);
}
