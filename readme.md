# 💀 Skeleton #
Skeleton is a database-centric command-line code generation tool written in .net core. 

> When woodworkers are faced with the task of producing the same thing over and over, they cheat. They build themselves a jig or template. If they get the jig right once, they can reproduce a piece of work time after time. The jig takes away complexity and reduces the chances of making mistakes, leaving the craftsman free to concentrate on quality.

_David Thomas and Andy Hunt - The Pragmatic Programmer_

## Main Ideas ##
- Use all the schema information from a relational database
- Generated code should look as nice as human-written code.
- You should be able to re-generate your code multiple times.
- You should be able to opt out of re-generating particular files.
- Use sane conventions for naming and structure over configuration. 

## What Does Skeleton Generate ##
Skeleton attempts to generate a 'full stack' of an application once the database schema is defined. Generating a full application is usually infeasible because some parts of an application will be particular to that application. Skeleton tries to create a good base to customise from. It generates:
- database functions and types for function results
- database policies for controlling entity access
- C# 'repository' wrappers for database functions
- C# controllers that call through to repositories
- typescript client API for calling C# controllers
- React components for displaying, editing, and deleting entities.
- In-memory repositories to make tests easier to set up

## Databases ##
Relational databases provide a rich source of machine-readable information about the entities and their relationships in a domain. Skeleton uses this, with some augmentation via attributes, to generate the basics of an application. Skeleton is currently very postgres-centric, but could be enhanced to support other databases in the future.

## Pre-Requisites ##
Skeleton and the apps it generates depend on the following:
1. Postgres - if you don't have it installed a docker image is probably the easiest way to get started.
2. The .NET SDK
3. Node (for building react front-ends)
4. Git

## Getting Started ##
To test out Skeleton using the Survey sample application perform the following steps. 
1. Install the pre-requisites mentioned above
1. Clone the Skeleton repository
1. Build the Skeleton solution
1. Clone the template project base [TODO - provide location]
1. Run the Survey app .sql file `./Skeleton.Tests/scripts/survey/001 - survey.sql`. Note down the password that is generated for the user survey_web_user in the output. You will use this later in step 8.
1. Create a configuration file called `survey.config.json` for the survey application in the build output directory of the Skeleton.console project (probably `./Skeleton.Console/bin/debug/netcoreapp3.1/`) . You will probably want to customize the "root" location
```
{
  "root": "c:\\path\\to\\your\\app",
  "name": "SurveyApp",
  "data-dir": ".\\Data\\Database\\",
  "ConnectionStrings": {
    "application-db": "Server=127.0.0.1;Port=5432;Database=survey;User Id=postgres;Password=secret_password;"
  }
}
```
7. Run the Skeleton console by navigating to the Skeleton.Console app build output directory and running `skeleton-codegen -u -react -c survey -n --tmplt <path to template project from step 6 above> --brand-color #5FD980` 
1. Open the root location specified in the config file you created in step 6 above, and open the `appsettings.json`. Fix up the connection string. The script `001 - survey.sql` creates a user called survey_web_user, and generates a random password for the user which it prints to the output. Assuming you're connecting to a local postgres db running on the standard port the connection string would be `"Server=127.0.0.1;Port=5432;Database=survey;User Id=survey_web_user;Password=<output from script>;"`
1. Build your new application. Building the app for the first time takes a while, because it does an NPM restore to build the react front-end.
1. To re-generate your app after a db schema change run the .sql file located in .sql file `./Skeleton.Tests/scripts/survey/002 - additional fields.sql`
1. Re-generate your application from the Skeleton.Console folder (the same location as step 7 above) by running `skeleton-codegen -u -react -c survey -del`

### Command-Line Arguments ###
```
      --brand-color=VALUE    Brand Color for new project. Only applicable when -n or --new option is specified
  -c, --config=VALUE         JSON configuration file to use.
  	  --client-dir, --client-code-directory=VALUE
  	  						 the directory to generate client code into.
      --data-dir, --database-code-directory=VALUE
                             the root directory to generate database code into.
      --data-test-dir, --database-test-directory=VALUE
                             the root directory to generate database test helpers into.
      --dbg, --debug         Attach Debugger on start
      --del                  delete generated files before re-generating
      --flutter              Generate a Flutter client for application
  -h, -?, --help             show this message and exit
      --logo=VALUE           SVG logo for new project. Only applicable when -n or --new option is specified
      --name=VALUE           Name of the application. Used for default C# namespace for generated items
  -n, --new                  Generate a new project
      --no-policy            Globally disable generation of security policies
      --no-test-repo         Disable generation of test repositories
  -r, --root=VALUE           the root folder to generate code into.
      --react                Change the web UI generated to be React
      --test-data=VALUE      Generate test data of the specified size for empty tables.
      --tmplt=VALUE          Template project directory
  -t, --type=VALUE           Only generate for a single type (for debugging)
  -u, --update-db-operations Update database with generated operations
  -v                         increase debug message verbosity
  -x, --exclude=VALUE        Exclude schema
```


## Configuration ##
Skeleton uses a combination of command-line arguments and configuration to control its behaviour. Longer, infrequently changing, and more tedious to type settings like paths and database connection strings should be stored config. 

You can specify the configuration file to use with the `-c` command-line parameter. For example if passed `-c foo` Skeleton will look for a configuration file called `foo.codegen.json`.  

Here is an example codegen.json config file:

```
{
  "root": "c:\\temp\\NewProject\\app",
  "name": "AppName",
  "data-dir": ".\\Data\\Database\\",
  "data-test-dir": ".\\Data.Testing\\",
  "ConnectionStrings": {
    "application-db": "Server=127.0.0.1;Port=5432;Database=database_name;User Id=code_gen_user;Password=secret_password;"
  }
}
```

### Base Project ###

### Re-Generating Code ###
Skeleton is designed to allow you to re-generate your code many times. Each generated file has a small comment at the start of it that signals that it was generated by Skeleton. If you make manual modifications to a file and no-longer want it re-generated you should remove this comment. There are also command-line switches for deleting generated code, which you can use to purge generated code that is no-longer required. Some of the generated code is also factored in such a way that customizations can be made in smaller, targeted files (e.g. UI validation), so that re-generation can still occur for larger ones.

## Schema Conventions ##
- datetime fields with names `created` and `updated` will be used for change tracking, if present
- the presence of a datetime field called `deleted_date` will allow "soft delete" of items in a table
- a table called attachments with a byte[] field will be treated as a table for uploading/storing files, or you can add an attribute called isAttachment and set to true
- the first text field of an entity will be used as the 'display' field for that entity.
- a field called `search_content` of type tsvector will be automatically updated with all text values for the entity when it is saved (so it can be searched). Search UI for this entity will also be generated.

## Attributes ##
Attributes are set as a JSON text string 'comment' on the respective database entity.

### Entity/Table Level ###
- hardDelete: true|false - When set to true generates an operation to 'hard' delete the entity. Defaults to false.
- ignore: true|false - ignores an entity for code-generation purposes. Defaults to false.
- important: true|false - flags the type as important. Doing this will add it to the 'home' screen for ordinary users.
- ui: true|false - When set to false suppresses the generation of any UI for this entity. Default to true.
- api: true|false - When set to false supresses the generation of any controller API for this entity. Default to true. If 'api' is set to false UI generation is also disabled, as if the 'ui' attribute (above) was set to false.
- isSecurityPrincipal: true|false - flags a type as being the type the app will use for security tracking, and will get from the HttpContext.User. Defaults to false. 
- createPolicy: true|false - when set to false no security policy will be created. Default is true.
- type - 'reference' causes ApplicationType IsReference to return true. Used in the creation of security policies.
- noAddUI: true|false - suppress creation of add UI for that type. Defaults to false.
- noEditUI: true|false - suppress creation of edit UI for that type. Defaults to false.
- isAttachment: true|false - when set to true is treated specially as a table for uploading/storing files. Defaults to false.
- security: an array of roles with the rights that they have. There are 3 built-in 'roles' - 'user' (authenticated users), 'admin' (administrators) and 'anon' (anonymous/unauthenticated users). If no security information is provided then administrators can view, add, edit and delete (if the entity allows it) all entities, authenticated users can view reference data, add data, and edit/delete data they created. Anonymous users cannot do anything. The rights that can be assigned at the entity level are: read, read-all, list, add, edit, edit-all, and delete. The difference between read and read-all is that read allows a type of user to view the items they created, whereas read-all allows them to view all entities of that type. Edit and edit-all are similar. Security is implemented as a combination of row-level security (RLS) policy and attributes on ASPNET controllers. An example security setting that would allow anonymous users read `{"security":{"anon":["read"]}}`
- apiHooks: "none"|"modify"|"all" - When set to "modify" any API methods that changes or creates new data will generate an API hook. When set to "all", all API operations will generate pre and post-execution hooks. Defaults to "none", which means no hooks will be generated. 
- apiConstructor: "generate"|"none" - when set to "generate" a constructor is generated for the API controller. When set to none no constructur is generated, so a custom constructor can be added to a partial class. Defaults to "generate".
- addMany: true|false - When set to true creates array-based add/insert operations to allow multiple entities to be added via a single call. Defaults to false.
- apiControllerBaseClass: string - the name of the custom controller that the API controller should inherit from. Defaults to `BaseController` (a type which you will need to create).
- paged: true|false - When set to true creates paged select all/select all for display operations for this type. Defaults to false.

### Function Level ###
- applicationtype:[type name] on a function acts as a hint that that type should be returned by the function wrapper
- changesData: true|false - the function changes data (used to generate validation). Defaults to false. 
- createsNew: true|false - the function creates a new entity. Defaults to false.
- generated: true|false - used by code-gen infrastructure to determine if it should be dropped on re-generation.  Defaults to false.
- returnTypeName:[type name] - custom type name for query result. If this attribute is not set the name of the query is appended with the word 'Result'
- isSearch: true|false - Identifies 'search' operations that receive some special UI treatment. Defaults to false.
- navParams:string[] - list of parameters provided via navigation rather than entered by the user.
- isDelete: true|false - Identifies 'delete' operation. Defaults to false.
- ui: true|false - When set to false suppresses the generation of any UI for this operation. Default to true.
- api: true|false - when set to false suppresses the generation of anything relating to this operation at the API layer. Defaults to true;
- apiHooks: true|false - when set to true custom 'before' and 'after' methods are called prior to the generated API code being called. Defaults to false.
- single_result: true|false - when set to true it causes the generated repository and API operations to return singular items instead of lists. Defaults to false.
- fullName: string - used to get around the 63 byte length limit of postgres entities. The pattern of <entity>_<operation> often leads to names greater than 63 characters.
- paged: true|false - indicates that the operation supports paging. Defaults to false.

### Field Level ### 
- largeContent : true|false - Signals to UI generators when set to true that a particular field should be displayed with more screen area. Defaults to false.
- rank: number - used to order fields in UI
- isRating: true|false - if true (and applied to a numeric field) the UI changes to show a a 1-5 star input widget. Defaults to false.
- isContentType: true|false - if true (on an entity that is an attachment/file) it stores the MIME/content-type the item will be served up as, and is set from the inbound content stream. Defaults to false.
- type: 'color', 'thumbnail' - when set to color indicates that the field stores a color value. The UI changes to reflect this.
							 - when set to 'thumbnail' (on an entity that is an attachment/file) it stores a thumbnail of the attachment/file.
							 Defaults to empty.
- add: true|false - When set to false excludes the field from add operations and from add UI. Default to true.
- edit: true|false - When set to false excludes the field from edit operations and from edit UI. Default to true.
- isDisplayForType: true|false - When set to true this field becomes the 'summary' that is shown in other places when referring to that type. For example a 'Name' field might be isDisplayType:true for a product or person. Defaults to false.

Postgres SQL Syntax for adding comments to fields is
```sql
COMMENT ON COLUMN public.foo.bar IS '{"rank": 1}';
```

### Switching to AAD for Auth ###
The generated solution uses IdentityServer for Authentication by default. To switch to AAD for authentication perform the following steps.
- add a reference to nuget package `Microsoft.Identity.Web`
- modify startup.cs to remove IdentityServer calls
More info is available [in this quick-start](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-v2-aspnet-core-webapp)

## Database Construct Support

| Construct             | Postgres     | SQL Server                    |
|-----------------------|--------------|-------------------------------|
| Integer Primary Kes   | ✅            | ✅                             |
| Auto-incrementing PKs | ✅ (sequence) | ✅ (identity)                  |
| Non-integer primary keys | ✅ | ✅                             |
| Composite Primary Keys | ❌ | ❌                             |
| Functions | ✅ | ✅                             |
| Stored Procedures | N/A | ✅                             |
| Views | ? | Excluded from generated model |
