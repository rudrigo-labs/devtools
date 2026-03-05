# Testes Manuais Pendentes - 2026-03-05

Objetivo: listar exatamente o que ainda precisa de validacao manual para fechamento total da versao.

## 1. Ngrok E2E (obrigatorio para fechar 100%)

Pre-condicoes:

1. ter `ngrok` instalado
2. conta ngrok ativa
3. authtoken valido

## 1.1 Preparacao

1. confirmar no terminal:

```powershell
ngrok version
```

2. abrir DevTools > Ferramentas > Ngrok
3. se aparecer onboarding, preencher e salvar token

## 1.2 Caso de teste A - Start/Stop basico

Passos:

1. informar porta local (ex.: `5000`)
2. clicar `Iniciar Tunel`
3. aguardar status de sucesso
4. verificar se lista de tuneis foi carregada
5. clicar `Parar Tunel`

Esperado:

- tunel inicia sem erro
- URL publica aparece na lista
- stop remove/encerra tunel

## 1.3 Caso de teste B - Kill all

Passos:

1. iniciar pelo menos 1 tunel
2. clicar `Kill All`
3. confirmar no dialogo

Esperado:

- tuneis finalizados
- status atualizado

## 1.4 Caso de teste C - sem binario instalado

Passos:

1. remover ngrok do PATH (ou usar maquina sem ngrok)
2. abrir janela Ngrok

Esperado:

- status indica ngrok nao encontrado
- botoes de start/stop bloqueados
- onboarding continua acessivel

## 2. Cenario skipped 1 - PathSelector

Teste ignorado automatizado:

- `PathSelectorTests.SelectedPath_Updates_TextBox_Display`

Validacao manual:

1. abrir ferramenta com `PathSelector` (Organizer, Snapshot, Migrations)
2. selecionar caminho
3. validar campo mostrando caminho correto
4. fechar e reabrir ferramenta
5. validar se ultimo caminho foi mantido quando aplicavel

## 3. Cenario skipped 2 - Snapshot persistencia

Teste ignorado automatizado:

- `SnapshotWindowTests.ProcessButton_Persists_SelectedPath_To_Settings`

Validacao manual:

1. abrir Snapshot
2. selecionar pasta valida
3. gerar snapshot
4. fechar Snapshot
5. reabrir Snapshot

Esperado:

- campo de pasta reaparece com ultimo valor usado

## 4. Validacao de release recomendada

Depois dos testes acima:

1. rodar build

```powershell
dotnet build src/DevTools.slnx -c Debug
```

2. rodar testes automatizados

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal
```

3. registrar resultado com data/hora

## 5. Resultado desta rodada (referencia)

- Automatizados: 36 aprovados, 2 ignorados, 0 falhas
- Pendente manual: Ngrok E2E + 2 cenarios skipped
