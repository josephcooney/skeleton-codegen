CREATE TABLE "user" (
                        id serial primary key NOT NULL,
                        "name" text NULL,
                        is_system bool NOT NULL,
                        user_name text NOT NULL,
                        created timestamp with time zone not null NOT NULL,
                        created_by int NOT NULL references "user"(id),
                        CONSTRAINT user_name_unique UNIQUE (user_name)
);

insert into "user" (id, name, is_system, user_name, created, created_by)
values (1, 'System', true, 'System', clock_timestamp(), 1);

ALTER SEQUENCE user_id_seq RESTART WITH 2;

COMMENT ON TABLE public.user IS '{"noAddUI":true, "noEditUI":true, "isSecurityPrincipal":true, "createPolicy":false}';

create table office_logo (
                             id serial primary key not null,
                             name text not null,
                             mime_type text not null,
                             contents bytea not null,
                             thumbnail bytea not null,
                             created_by int references "user" (id) not null,
                             created timestamp with time zone not null,
                             modified_by int references "user"(id),
                             modified timestamp with time zone
);
COMMENT ON TABLE public.office_logo IS '{"isAttachment":true, "security":{"anon":["read"]}}'; -- anon-read is a bit of a hack
COMMENT ON COLUMN public.office_logo.mime_type IS '{"isContentType": true}';

create table office (
                        id serial primary key not null,
                        name text not null,
                        office_logo_id int references office_logo(id),
                        created_by int references "user" (id) not null,
                        created timestamp with time zone not null,
                        modified_by int references "user"(id),
                        modified timestamp with time zone
);

COMMENT ON TABLE public.office IS '{"type":"reference"}';

create table room (
                      id serial primary key not null,
                      name text not null,
                      office_id int not null references office(id),
                      created_by int references "user" (id) not null,
                      created timestamp with time zone not null,
                      modified_by int references "user"(id),
                      modified timestamp with time zone
);

COMMENT ON TABLE public.room IS '{"type":"reference"}';

create table desk_type (
                           id serial primary key not null,
                           name text not null,
                           width int not null,
                           depth int not null,
                           monitor_count int not null,
                           created_by int references "user" (id) not null,
                           created timestamp with time zone not null,
                           modified_by int references "user"(id),
                           modified timestamp with time zone
);

COMMENT ON TABLE public.desk_type IS '{"type":"reference"}';

create table desk_orientation (
                                  id serial primary key not null,
                                  name text not null,
                                  created_by int references "user" (id) not null,
                                  created timestamp with time zone not null,
                                  modified_by int references "user"(id),
                                  modified timestamp with time zone
);

COMMENT ON TABLE public.desk_orientation IS '{"type":"reference"}';

insert into desk_orientation(id, name, created_by, created)
values (1, 'Up', 1, clock_timestamp());

insert into desk_orientation(id, name, created_by, created)
values (2, 'Down', 1, clock_timestamp());

insert into desk_orientation(id, name, created_by, created)
values (3, 'Left', 1, clock_timestamp());

insert into desk_orientation(id, name, created_by, created)
values (4, 'Right', 1, clock_timestamp());

ALTER SEQUENCE desk_orientation_id_seq RESTART WITH 5; -- users probably shouldn't be adding values here anyway, but...

create table desk (
                      id serial primary key not null,
                      room_id int not null references room(id),
                      desk_type_id int not null references desk_type(id),
                      desk_orientation_id int not null references desk_orientation(id),
                      x_coord int not null,
                      y_coord int not null,
                      notes text,
                      created_by int references "user" (id) not null,
                      created timestamp with time zone not null,
                      modified_by int references "user"(id),
                      modified timestamp with time zone
);

COMMENT ON TABLE public.desk IS '{"type":"reference"}';

create table desk_booking (
                              id serial primary key not null,
                              desk_id int not null references desk(id),
                              booking_date date not null,
                              confirmation_timestamp timestamp with time zone,
                              reminder_sent_timestamp timestamp with time zone,
                              created_by int references "user" (id) not null,
                              created timestamp with time zone not null,
                              modified_by int references "user"(id),
                              modified timestamp with time zone,
                              deleted_date timestamp with time zone,
                              deleted_by int references "user"(id)
);

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

DO
$$
    declare user_pwd text;
BEGIN
        user_pwd = MD5(random()::text);

        raise notice 'desky_web_user password set to %', user_pwd;

DROP ROLE if EXISTS desky_web_user;
EXECUTE format('CREATE USER desky_web_user PASSWORD %L', user_pwd);

END
$$;

GRANT web_app_role TO desky_web_user;

grant usage, select on user_id_seq to web_app_role;
grant usage, select on office_logo_id_seq to web_app_role;
grant usage, select on office_id_seq to web_app_role;
grant usage, select on room_id_seq to web_app_role;
grant usage, select on desk_type_id_seq to web_app_role;
grant usage, select on desk_orientation_id_seq to web_app_role;
grant usage, select on desk_id_seq to web_app_role;
grant usage, select on desk_booking_id_seq to web_app_role;

SET search_path TO "public";

drop function if exists user_select_current;

CREATE OR REPLACE FUNCTION user_select_current (
    security_user_id_param integer
)
    RETURNS SETOF "user" AS
$$

BEGIN

RETURN QUERY
SELECT
    "user".id,
    "user"."name",
    "user".is_system,
    "user".user_name,
    "user".created,
    "user".created_by
FROM "user"
WHERE
        "user".id = security_user_id_param ;

END
$$
LANGUAGE plpgsql VOLATILE SECURITY INVOKER
                     COST 100;

REVOKE ALL ON FUNCTION user_select_current (integer ) FROM public;

GRANT EXECUTE ON FUNCTION user_select_current ( integer ) TO web_app_role;

COMMENT ON FUNCTION user_select_current ( integer )
    IS '{"applicationtype":"user", "generated":false, "fullName":"user_select_current" , "single_result":true }';


SET search_path TO "public";

drop function if exists user_select_by_login;

CREATE OR REPLACE FUNCTION user_select_by_login (
    "name" text,
    user_name text
)
    RETURNS int AS
$$
DECLARE user_id int;

BEGIN

select id into user_id from "user"
where "user".user_name = user_select_by_login.user_name;

IF (user_id is not null) THEN
        return user_id;
END IF;

    user_id = nextval(pg_get_serial_sequence('user', 'id'));

insert into "user" (id, "name", is_system, user_name, created, created_by)
values (user_id, user_select_by_login.name, false, user_select_by_login.user_name, clock_timestamp(), user_id);

return user_id;

END
$$
LANGUAGE plpgsql VOLATILE SECURITY INVOKER
COST 100;

REVOKE ALL ON FUNCTION user_select_by_login (text, text) FROM public;

GRANT EXECUTE ON FUNCTION user_select_by_login (text, text) TO web_app_role;

COMMENT ON FUNCTION user_select_by_login ( text, text)
    IS '{"applicationtype":"user", "generated":false, "api":false }';

CREATE SCHEMA IF NOT EXISTS hangfire AUTHORIZATION web_app_role;




