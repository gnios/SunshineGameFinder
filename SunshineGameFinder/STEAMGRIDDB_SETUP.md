# Configuração do SteamGridDB

## Visão Geral

O SunshineGameFinder agora utiliza o **SteamGridDB** para buscar e baixar automaticamente as imagens de capa dos jogos. O SteamGridDB é uma plataforma comunitária que oferece imagens de alta qualidade para jogos.

## Como Funciona

### Busca Automática

A aplicação busca imagens para os jogos de duas formas:

1. **Por Steam App ID**: Se o jogo estiver instalado numa biblioteca Steam, a aplicação tenta extrair o Steam App ID do arquivo `appmanifest_*.acf` e usa esse ID para buscar a imagem no SteamGridDB.

2. **Por Nome do Jogo**: Se o Steam App ID não estiver disponível, a aplicação busca pelo nome do jogo usando a busca autocomplete do SteamGridDB.

### Seleção de Imagens

Quando múltiplas imagens estão disponíveis, a aplicação prioriza:
1. Imagens verificadas pela comunidade
2. Imagens com maior pontuação
3. A primeira imagem disponível

## Configuração da API Key (Opcional)

Embora a API do SteamGridDB funcione sem autenticação para buscas básicas, você pode configurar uma chave de API para acessar recursos adicionais e evitar limites de taxa.

### Obter uma API Key

1. Acesse [SteamGridDB](https://www.steamgriddb.com/)
2. Crie uma conta ou faça login
3. Vá em **Preferências do Usuário** > **API**
4. Gere sua chave de API

### Configurar a API Key

Defina a variável de ambiente `STEAMGRIDDB_API_KEY`:

**Windows (PowerShell):**
```powershell
$env:STEAMGRIDDB_API_KEY = "sua_chave_aqui"
```

**Windows (Command Prompt):**
```cmd
set STEAMGRIDDB_API_KEY=sua_chave_aqui
```

**Linux/macOS:**
```bash
export STEAMGRIDDB_API_KEY=sua_chave_aqui
```

Para tornar permanente, adicione ao arquivo de perfil do sistema (`.bashrc`, `.zshrc`, variáveis de ambiente do Windows, etc.).

## Estrutura do Código

### Arquivos Principais

- **`Services/ImageScraper.cs`**: Classe principal que coordena a busca de imagens
- **`Services/SteamGridDb/SteamGridDbService.cs`**: Implementação da integração com a API do SteamGridDB
- **`Services/SteamAppIdExtractor.cs`**: Extrai o Steam App ID dos arquivos manifest
- **`Services/GameScanner.cs`**: Integra a busca de imagens durante o escaneamento de jogos

### Endpoints Utilizados

- `GET /api/v2/games/steam/{steamAppId}`: Busca informações do jogo por Steam App ID
- `GET /api/v2/search/autocomplete/{query}`: Busca jogos por nome
- `GET /api/v2/grids/game/{gameId}`: Obtém imagens de grid para um jogo específico

## Resolução de Problemas

### Imagens Não São Baixadas

1. Verifique sua conexão com a internet
2. Confirme que o nome do jogo está correto (limpe caracteres especiais)
3. Verifique se há limites de taxa da API (configure uma API key)

### Steam App ID Não É Detectado

O Steam App ID só pode ser extraído para jogos instalados através do Steam. Para outros jogos, a busca por nome será utilizada automaticamente.

### Erros de Permissão

Certifique-se de que a aplicação tem permissão para criar diretórios e escrever arquivos na pasta `covers`.

## Mais Informações

- [Documentação da API do SteamGridDB](https://www.steamgriddb.com/api/v2)
- [SteamGridDB no GitHub](https://github.com/steamgriddb)
