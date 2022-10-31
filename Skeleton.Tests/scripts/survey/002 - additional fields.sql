alter table survey
	add banner_color char(7);

COMMENT ON COLUMN public.survey.banner_color IS '{"type": "color"}';