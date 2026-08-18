parser grammar GhostShaderParser;

options {
    tokenVocab = GhostShaderLexer;
}

// Top-level rule
shaderFile: (topLevelDeclaration | moduleDeclaration | shaderProjectDeclaration)+ EOF;

moduleDeclaration:
    MODULE STRING_LITERAL LBRACE
        moduleItem*
    RBRACE;

moduleItem:
    importDeclaration
    | interfaceDeclaration
    | implementationDeclaration
    | templateDeclaration
    | shaderDeclaration
    ;

shaderProjectDeclaration:
    SHADER_PROJECT STRING_LITERAL LBRACE
        projectItem*
    RBRACE;

projectItem:
    MODULE STRING_LITERAL SEMICOLON
    | TARGET STRING_LITERAL SEMICOLON
    ;

topLevelDeclaration:
    importDeclaration
    | interfaceDeclaration
    | implementationDeclaration
    | templateDeclaration
    | shaderDeclaration
    | passBlock
    ;

importDeclaration:
    IMPORT STRING_LITERAL SEMICOLON;

interfaceDeclaration:
    EXPORT? CLOSED? INTERFACE interfaceScope qualifiedIdentifier opaqueBracedBody? SEMICOLON?;

interfaceScope:
    PIPELINE
    | SHADER
    ;

implementationDeclaration:
    EXPORT? IMPLEMENTATION qualifiedIdentifier COLON qualifiedIdentifier opaqueBracedBody;

templateDeclaration:
    EXPORT? TEMPLATE qualifiedIdentifier LBRACE
        templateBody
    RBRACE;

templateBody:
    (propertiesBlock | slotBlock | passBlock | pipelineBlock | shaderModel | functionCall)*;

propertiesBlock:
    PROPERTIES LBRACE
        propertyDeclaration*
    RBRACE;

propertyDeclaration:
    propertyType identifier (LBRACK NUMBER RBRACK)? SEMICOLON;

propertyType:
    identifier;

slotBlock:
    SLOT LBRACE
        slotItem*
    RBRACE;

slotItem:
    qualifiedIdentifier (EQUALS qualifiedIdentifier)? SEMICOLON;

shaderDeclaration:
    EXPORT? SHADER qualifiedIdentifier (COLON qualifiedIdentifier)? LBRACE
        shaderBody
    RBRACE;

shaderBody:
    (
        shaderModel
        | propertiesBlock
        | payloadBlock
        | implementationDeclaration
        | bindBlock
        | pipelineBlock
        | passBlock
        | functionCall
    )*;

payloadBlock:
    PAYLOAD opaqueBracedBody;

bindBlock:
    BIND LBRACE
        bindItem*
    RBRACE;

bindItem:
    qualifiedIdentifier EQUALS qualifiedIdentifier SEMICOLON;

shaderModel:
    SM shaderModelIdentifier SEMICOLON;

shaderModelIdentifier:
    identifier
    | NUMBER
    | NUMBER identifier
    | STRING_LITERAL
    ;

// Pipeline block
pipelineBlock:
    PIPELINE LBRACE
        pipelineStatement*
    RBRACE;

pipelineStatement:
    identifier EQUALS identifier SEMICOLON;

// Pass block
passBlock:
    PASS qualifiedIdentifier LBRACE
        passBody
    RBRACE;

passBody:
    (composeBlock | definesBlock | includesBlock | keywordsBlock | pipelineBlock | hlslBlock | shaderEntry | functionCall)*;

composeBlock:
    COMPOSE LBRACE
        composeItem*
    RBRACE;

composeItem:
    qualifiedIdentifier SEMICOLON;

definesBlock:
    DEFINES LBRACE
        defineStatement*
    RBRACE;

defineStatement:
    identifier SEMICOLON;

includesBlock:
    INCLUDES LBRACE
        includeStatement*
    RBRACE;

includeStatement:
    STRING_LITERAL SEMICOLON;

keywordsBlock:
    KEYWORDS LBRACE
        keywordStatement*
    RBRACE;

keywordStatement:
    identifier (COMMA identifier)* SEMICOLON;

hlslBlock:
    HLSL opaqueBracedBody;

opaqueBracedBody:
    LBRACE
        opaqueInner*
    RBRACE;

opaqueInner:
    ~(LBRACE | RBRACE)
    | opaqueBracedBody
    ;

shaderEntry:
    identifier STRING_LITERAL COLON STRING_LITERAL SEMICOLON;

functionCall:
    identifier LPAREN functionArguments? RPAREN SEMICOLON;

functionArguments:
    functionArgument (COMMA functionArgument)*;

functionArgument:
    STRING_LITERAL | NUMBER | qualifiedIdentifier;

identifier:
    IDENTIFIER
    | GLOBAL
    | LOCAL
    | PASS
    | SHADER
    | PIPELINE
    | TEMPLATE
    | INTERFACE
    | IMPLEMENTATION
    | MODULE
    | IMPORT
    | EXPORT
    | CLOSED
    | SLOT
    | BIND
    | COMPOSE
    | PAYLOAD
    | TARGET
    | PROVIDER
    | PROPERTIES
    ;

qualifiedIdentifier:
    identifier (DOT identifier)*
    | STRING_LITERAL
    ;
