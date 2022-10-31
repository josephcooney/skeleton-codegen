CREATE TABLE public."AspNetRoles"
(
    "Id" text COLLATE pg_catalog."default" NOT NULL,
    "Name" character varying(256) COLLATE pg_catalog."default",
    "NormalizedName" character varying(256) COLLATE pg_catalog."default",
    "ConcurrencyStamp" text COLLATE pg_catalog."default",
    CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."AspNetRoles"
    IS '{"ignore":true}';

CREATE UNIQUE INDEX "RoleNameIndex"
    ON public."AspNetRoles" USING btree
    ("NormalizedName" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

CREATE TABLE public."AspNetRoleClaims"
(
    "Id" integer NOT NULL,
    "RoleId" text COLLATE pg_catalog."default" NOT NULL,
    "ClaimType" text COLLATE pg_catalog."default",
    "ClaimValue" text COLLATE pg_catalog."default",
    CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId")
        REFERENCES public."AspNetRoles" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."AspNetRoleClaims"
    IS '{"ignore":true}';

CREATE INDEX "IX_AspNetRoleClaims_RoleId"
    ON public."AspNetRoleClaims" USING btree
    ("RoleId" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

CREATE TABLE public."AspNetUsers"
(
    "Id" text COLLATE pg_catalog."default" NOT NULL,
    "UserName" character varying(256) COLLATE pg_catalog."default",
    "NormalizedUserName" character varying(256) COLLATE pg_catalog."default",
    "Email" character varying(256) COLLATE pg_catalog."default",
    "NormalizedEmail" character varying(256) COLLATE pg_catalog."default",
    "EmailConfirmed" boolean NOT NULL,
    "PasswordHash" text COLLATE pg_catalog."default",
    "SecurityStamp" text COLLATE pg_catalog."default",
    "ConcurrencyStamp" text COLLATE pg_catalog."default",
    "PhoneNumber" text COLLATE pg_catalog."default",
    "PhoneNumberConfirmed" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "LockoutEnabled" boolean NOT NULL,
    "AccessFailedCount" integer NOT NULL,
    CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."AspNetUsers"
    IS '{"ignore":true}';

CREATE INDEX "EmailIndex"
    ON public."AspNetUsers" USING btree
    ("NormalizedEmail" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

CREATE UNIQUE INDEX "UserNameIndex"
    ON public."AspNetUsers" USING btree
    ("NormalizedUserName" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

CREATE TABLE public."AspNetUserClaims"
(
    "Id" integer NOT NULL,
    "UserId" text COLLATE pg_catalog."default" NOT NULL,
    "ClaimType" text COLLATE pg_catalog."default",
    "ClaimValue" text COLLATE pg_catalog."default",
    CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId")
        REFERENCES public."AspNetUsers" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."AspNetUserClaims"
    IS '{"ignore":true}';

CREATE INDEX "IX_AspNetUserClaims_UserId"
    ON public."AspNetUserClaims" USING btree
    ("UserId" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

CREATE TABLE public."AspNetUserLogins"
(
    "LoginProvider" character varying(128) COLLATE pg_catalog."default" NOT NULL,
    "ProviderKey" character varying(128) COLLATE pg_catalog."default" NOT NULL,
    "ProviderDisplayName" text COLLATE pg_catalog."default",
    "UserId" text COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId")
        REFERENCES public."AspNetUsers" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."AspNetUserLogins"
    IS '{"ignore":true}';

CREATE INDEX "IX_AspNetUserLogins_UserId"
    ON public."AspNetUserLogins" USING btree
    ("UserId" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

CREATE TABLE public."AspNetUserRoles"
(
    "UserId" text COLLATE pg_catalog."default" NOT NULL,
    "RoleId" text COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId")
        REFERENCES public."AspNetRoles" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId")
        REFERENCES public."AspNetUsers" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."AspNetUserRoles"
    IS '{"ignore":true}';

CREATE INDEX "IX_AspNetUserRoles_RoleId"
    ON public."AspNetUserRoles" USING btree
    ("RoleId" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

CREATE TABLE public."AspNetUserTokens"
(
    "UserId" text COLLATE pg_catalog."default" NOT NULL,
    "LoginProvider" character varying(128) COLLATE pg_catalog."default" NOT NULL,
    "Name" character varying(128) COLLATE pg_catalog."default" NOT NULL,
    "Value" text COLLATE pg_catalog."default",
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId")
        REFERENCES public."AspNetUsers" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."AspNetUserTokens"
    IS '{"ignore":true}';

CREATE TABLE public."DeviceCodes"
(
    "UserCode" character varying(200) COLLATE pg_catalog."default" NOT NULL,
    "DeviceCode" character varying(200) COLLATE pg_catalog."default" NOT NULL,
    "SubjectId" character varying(200) COLLATE pg_catalog."default",
    "ClientId" character varying(200) COLLATE pg_catalog."default" NOT NULL,
    "CreationTime" timestamp without time zone NOT NULL,
    "Expiration" timestamp without time zone NOT NULL,
    "Data" character varying(50000) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT "PK_DeviceCodes" PRIMARY KEY ("UserCode")
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."DeviceCodes"
    IS '{"ignore":true}';

CREATE UNIQUE INDEX "IX_DeviceCodes_DeviceCode"
    ON public."DeviceCodes" USING btree
    ("DeviceCode" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

CREATE TABLE public."PersistedGrants"
(
    "Key" character varying(200) COLLATE pg_catalog."default" NOT NULL,
    "Type" character varying(50) COLLATE pg_catalog."default" NOT NULL,
    "SubjectId" character varying(200) COLLATE pg_catalog."default",
    "ClientId" character varying(200) COLLATE pg_catalog."default" NOT NULL,
    "CreationTime" timestamp without time zone NOT NULL,
    "Expiration" timestamp without time zone,
    "Data" character varying(50000) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT "PK_PersistedGrants" PRIMARY KEY ("Key")
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

COMMENT ON TABLE public."PersistedGrants"
    IS '{"ignore":true}';

CREATE INDEX "IX_PersistedGrants_SubjectId_ClientId_Type"
    ON public."PersistedGrants" USING btree
    ("SubjectId" COLLATE pg_catalog."default", "ClientId" COLLATE pg_catalog."default", "Type" COLLATE pg_catalog."default")
    TABLESPACE pg_default;

-- actual schema starts here

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

create table "user" (
	id serial primary key not null,
	name text,	
	handle text,
	aspnet_user_id text references "AspNetUsers"("Id")
);

insert into "user" (id, name) values (1, 'System');

ALTER SEQUENCE user_id_seq RESTART WITH 2;

COMMENT ON TABLE public.user IS '{"isSecurityPrincipal":true, "createPolicy":false}';

create table "image" (
	id serial primary key not null,
	name text not null,
	mime_type text not null,
	contents bytea not null,
	--
	created_by int references "user" (id) not null,
	created timestamp with time zone not null
);

COMMENT ON TABLE public.image IS '{"isAttachment":true, "security":{"anon":["read"]}}';
COMMENT ON COLUMN public.image.mime_type IS '{"isContentType": true}';

alter table "user"
	add profile_picture int references "image" (id);

create table message (
	id uuid primary key not null,
	contents text,
	image_id int references "image" (id),
	reply_to uuid references message(id),
	--
	created_by int references "user" (id) not null,
	created timestamp with time zone not null
);

create table "like" (
	id uuid primary key not null,
	message_id uuid not null references message(id),
	--
	created_by int references "user" (id) not null,
	created timestamp with time zone not null
);

create table follow (
	id uuid primary key not null,
	user_id int not null references "user"(id),
	--
	created_by int references "user" (id) not null,
	created timestamp with time zone not null
);

-- TODO 
-- re-tweet
-- attributes (esp. security) on things

DO
$$
BEGIN
   IF NOT EXISTS (
      SELECT                       
      FROM 
		pg_catalog.pg_roles
      WHERE 
		rolname = 'web_app_role') THEN

      CREATE ROLE web_app_role WITH
		NOLOGIN
		NOSUPERUSER
		NOCREATEDB
		NOCREATEROLE
		INHERIT
		NOREPLICATION
		CONNECTION LIMIT -1;
   END IF;
END
$$;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO web_app_role;

CREATE USER twitterlike_web_user WITH encrypted password '2W19gyP_01n9gwA3as8';

GRANT web_app_role TO twitterlike_web_user;

grant usage, select on user_id_seq to web_app_role;
grant usage, select on image_id_seq to web_app_role;