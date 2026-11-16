# Linguagem HERMES

Lexer, parser, AST, interpretador e runtime da Linguagem HERMES de Sidney Rodrigues, provenientes do repositório HERMES-Language no commit 6f8673b.

O código incluído usa os mesmos namespaces HERMESLANG. A adaptação à Unity 6 usa namespaces em bloco para C# 9 e imports explícitos no lugar de imports implícitos do projeto .NET original. A entrada e as ferramentas de console do projeto original ficam fora da integração de jogo.

O jogo registra suas funções por IHermesFunction. Os terminais montam o código pelas lacunas previstas no GDD e executam o programa no interpretador durante os testes de telemetria.
