## New Project Command-Line
Skeleton.exe -u -c bins -x hangfire -del --no-policy -n -tmplt E:\EmptySolution\base --tmplt-brnch aad-auth --logo e:\bins-demo\logo.svg --brand-color #00bbd4

## Fix up appsettings.Development.json ##
- Change connection string
- Change azure AD settings

## Add Nuget Packages ##
Hangfire
Hangfire.PostgreSql

## Register Hangfire Background Worker in Autofac ##
- add to ConfigureContainer in startup.cs
`builder.RegisterType<BackgroundJobClient>().AsImplementedInterfaces();`
  
## Set up Mapping
`npm install react-map-gl`
getting started is here https://visgl.github.io/react-map-gl/docs/get-started/get-started

you can get a mapbox token from here https://account.mapbox.com/

## Set Startup Args  
`"commandLineArgs": "-u -c bins -x hangfire -del --no-policy"`