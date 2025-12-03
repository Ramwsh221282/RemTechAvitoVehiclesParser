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

CREATE TABLE IF NOT EXISTS avito_parser_module.pagination_evaluation
(
    id uuid primary key,
    current_page integer not null,
    max_page timestamptz not null
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
    REFERENCES avito_parser_module.pagination_evaluating_parsers
    ON DELETE CASCADE 
);
