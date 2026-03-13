# Notes - Guia de Telas

Este documento explica todas as telas do modulo `Notes` no `DevTools.Host.Wpf`.

Arquivos principais:
- `src/Host/DevTools.Host.Wpf/Views/NotesWorkspaceView.xaml`
- `src/Host/DevTools.Host.Wpf/Views/NotesWorkspaceView.xaml.cs`
- `src/Host/DevTools.Host.Wpf/Facades/NotesFacade.cs`
- `src/Tools/DevTools.Notes/Engine/NotesEngine.cs`

---

## 1) Visao geral de navegacao

A tela de Notes tem 2 modos principais:
- `Notas` (modo de execucao)
- `Configuracao`

No topo existe:
- seletor de configuracao (`ConfigurationsCombo`)
- botao `Notas`
- botao `Configuracao`

Dentro de `Notas` existem 2 sub-telas:
- `ListGrid` (lista de notas)
- `EditGrid` (editor da nota)

Fluxo normal:
1. Entrar em `Notas`
2. Ver lista (`ListGrid`)
3. Abrir nota (duplo clique ou Enter) -> editor (`EditGrid`)
4. Voltar para lista pelo botao de voltar

---

## 2) Tela Notas - Lista (`ListGrid`)

Objetivo:
- Exibir as notas da configuracao selecionada
- Permitir criar, abrir, excluir, exportar e importar

Componentes principais:
- `NotesStoragePathHint`: mostra a pasta local de armazenamento
- `NotesList`: lista de itens `NoteListItem`
- AppBar inferior da lista:
  - `Nova nota` (`AddButton_Click`)
  - `Exportar backup ZIP` (`ExportButton_Click`)
  - `Importar backup ZIP` (`ImportButton_Click`)

Como abrir uma nota:
- duplo clique no item da lista
- Enter com item selecionado

Eventos usados para abrir:
- `NotesList_MouseDoubleClick`
- `NotesListItem_MouseDoubleClick`
- `NotesList_KeyDown` (tecla Enter)

Importante:
- clique no botao de excluir do item nao abre a nota (protecao no code-behind)

Exclusao de item:
- botao de lixeira por item (`DeleteItemButton_Click`)
- pede confirmacao antes de excluir

---

## 3) Tela Notas - Editor (`EditGrid`)

Objetivo:
- Editar ou criar conteudo de uma nota

Campos principais:
- `NoteTitleInput`: titulo
- `ExtensionCombo`: `.md` ou `.txt`
- `NoteContentInput`: conteudo
- `DriveStatusBanner`: status de sincronizacao com Google Drive

AppBar do editor:
- `Voltar` (`BackButton_Click`) -> retorna para lista
- `Salvar` (`SaveNote_Click`) -> salva nota
- `Excluir` (`DeleteCurrentNote_Click`) -> exclui nota atual

Atalho:
- `Ctrl+S` salva quando `EditGrid` esta visivel

Comportamento de salvar:
- nota existente: `NotesAction.SaveNote`
- nota nova: `NotesAction.CreateItem`

---

## 4) Tela Configuracao (`ConfigurationPanel`)

Objetivo:
- Gerenciar configuracoes salvas da ferramenta Notes

Campos:
- `NameInput` (obrigatorio)
- `DescriptionInput`
- `LocalRootPathSelector` (pasta local das notas)
- `IsDefaultCheck`

Secao Google Drive:
- `GoogleDriveEnabledCheck`
- `CredentialsPathSelector`
- `FolderIdInput`
- `OAuthTokenCachePathSelector`
- `ConnectDrive_Click`
- `DisconnectDrive_Click`
- `ShowSetupGuide_Click` (abre guia em janela modal)

---

## 5) Action Bar superior (comportamento por modo)

Botoes:
- `Novo` (`ActionNew_Click`)
- `Salvar` (`ActionSave_Click`)
- `Excluir` (`ActionDelete_Click`)
- `Cancelar` (`ActionCancel_Click`)

No modo `Notas`:
- `Salvar` salva a nota atual

No modo `Configuracao`:
- `Salvar` salva a configuracao
- `Excluir` exclui a configuracao selecionada

Estado visual:
- controlado por `ApplyModeState()`

---

## 6) Integracao com backend (o que cada acao chama)

Camada UI:
- `NotesWorkspaceView`

Camada facade:
- `NotesFacade` decide raiz local e inicializa componentes de Notes + Drive

Camada engine:
- `NotesEngine` executa a acao (`NotesAction`)

Acoes suportadas:
- `ListItems`
- `LoadNote`
- `CreateItem`
- `SaveNote`
- `DeleteItem`
- `ExportZip`
- `ImportZip`

Validacao de request:
- `NotesRequestValidator`

---

## 7) Fluxos prontos para uso

### Fluxo A - Abrir e editar nota existente
1. Entrar em `Notas`
2. Selecionar configuracao
3. Duplo clique na nota (ou Enter)
4. Editar conteudo
5. Salvar

### Fluxo B - Criar nota nova
1. Entrar em `Notas`
2. Clicar em `Nova nota`
3. Preencher titulo e conteudo
4. Escolher extensao
5. Salvar

### Fluxo C - Backup
1. Exportar ZIP pela AppBar da lista
2. Importar ZIP pela AppBar da lista

### Fluxo D - Configurar Google Drive
1. Ir para `Configuracao`
2. Habilitar sincronizacao
3. Informar credentials, folder id e pasta de token
4. Conectar

---

## 8) Checklist rapido de comportamento esperado

- Ao abrir o modulo, deve carregar configuracoes e listar notas
- Duplo clique/Enter em item deve abrir `EditGrid`
- Botao de lixeira do item so exclui, nao abre editor
- `Ctrl+S` deve salvar no editor
- `Voltar` deve retornar para a lista
- Mudanca de configuracao deve recarregar lista

---

## 9) Dica de debug rapido (se algo "nao abre")

Verifique nesta ordem:
1. Se `NotesList.ItemsSource` esta preenchido
2. Se `NotesList.SelectedItem` contem `NoteListItem`
3. Se `OpenNoteAsync` retorna sucesso (`ReadResult` nao nulo)
4. Se `ShowEditView()` foi chamado
5. Se `EditGrid.Visibility` ficou `Visible`

