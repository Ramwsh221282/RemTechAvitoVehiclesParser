CREATE SCHEMA IF NOT EXISTS avito_parser_module;

CREATE TABLE IF NOT EXISTS avito_parser_module.parser_tickets
(
    id uuid primary key,    
    type varchar(256) not null,
    payload jsonb not null,
    created timestamptz not null,
    was_sent boolean,
    finished timestamptz    
);

CREATE TABLE IF NOT EXISTS avito_parser_module.work_stages
(
  id uuid primary key,
  name varchar(128) not null,
  created timestamptz not null,
  finished timestamptz
);

CREATE TABLE IF NOT EXISTS avito_parser_module.pagination_evaluating_parsers
(
    id uuid primary key,
    domain varchar(128) not null,
    type varchar(128) not null
);

CREATE TABLE IF NOT EXISTS avito_parser_module.pagination_evaluating_parser_links
(
    id uuid primary key,
    parser_id uuid not null,
    url text not null,
    was_processed boolean not null,
    current_page integer,
    max_page integer,
    CONSTRAINT parser_id_fk FOREIGN KEY(parser_id)
    REFERENCES avito_parser_module.pagination_evaluating_parsers(id)
    ON DELETE CASCADE 
);

CREATE TABLE IF NOT EXISTS avito_parser_module.catalogue_urls
(
    id uuid primary key,
    link_id uuid not null,
    url text not null,
    was_processed boolean not null,
    retry_count integer not null,
    CONSTRAINT link_id_fk FOREIGN KEY(link_id)
        REFERENCES avito_parser_module.pagination_evaluating_parser_links(id)
        ON DELETE CASCADE
);