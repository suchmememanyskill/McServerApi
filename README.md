# McServerApi

ASP.NET server to host minecraft servers via an API

### Environment Variables:

**Config__Memory**

Memory size in GB. Default is 4

**Config__JavaFlags**

Additional flags passed to the JVM. Default is ""

**Config__ApiPort**

Port that the API gets hosted on. Default is 8080

### Volumes:

Mapping any volume is optional, but data will be lost on shutdown

**app/__del_mc_maps**

All deleted maps are moved into this folder

**app/__jar_cache**

Minecraft server versions are cached in this folder

**app/__mc_maps**

Map storage. Used to store current available maps, configuration for additional minecraft server versions and current map selection

**app/__mc_server_template**

Server template. When a server is turned on, this folder is copied in as a base. By default this contains `server.properties`, `ops.json` and `eula.txt`. Feel free to specifically override those files also