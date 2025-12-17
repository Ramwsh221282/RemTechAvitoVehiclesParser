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
  name varchar(128) not null  
);

CREATE TABLE IF NOT EXISTS avito_parser_module.parsers
(
    id uuid primary key,
    domain varchar(128) not null,
    type varchar(128) not null
);

CREATE TABLE IF NOT EXISTS avito_parser_module.parser_links
(
    id uuid primary key,
    url text not null,
    was_processed boolean not null,
    retry_count integer not null
);

CREATE TABLE IF NOT EXISTS avito_parser_module.catalogue_urls
(
    url text primary key,
    was_processed boolean not null,
    retry_count integer not null
);

CREATE TABLE IF NOT EXISTS avito_parser_module.items
(
    id varchar(64) primary key,    
    url text not null,
    was_processed boolean not null,
    retry_count integer not null,
    price bigint not null,
    is_nds boolean not null,
    address varchar(512) not null,
    photos jsonb not null,
    title varchar(512),
    characteristics jsonb
);