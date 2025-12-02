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

