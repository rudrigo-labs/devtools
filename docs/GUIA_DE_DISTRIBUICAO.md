# 📦 Guia de Distribuição - DevTools

Este guia explica como compilar, publicar e distribuir o **DevTools** (versão Tray App/WPF) para uso em outras máquinas.

---

## 📋 Pré-requisitos de Build

Para gerar o executável, você precisa ter instalado:
*   **.NET 8.0 SDK** (para compilar).
*   Visual Studio 2022 ou VS Code (opcional, para build via interface).

---

## 🚀 Gerando o Executável (Publish)

A melhor forma de distribuir o DevTools é como um **aplicativo independente (Self-Contained)** ou **dependente de framework (Framework-Dependent)**.

### Opção 1: Framework-Dependent (Recomendado)
Gera um executável menor, mas exige que o usuário tenha o .NET 8 Runtime instalado.

**Comando (PowerShell):**
```powershell
cd src/Presentation/DevTools.Presentation.Wpf
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish/framework_dependent
```

*   **Resultado:** Um arquivo `DevTools.Presentation.Wpf.exe` único (mais alguns arquivos de configuração/native libs se necessário) na pasta `publish/framework_dependent`.
*   **Tamanho:** ~2-5 MB.

### Opção 2: Self-Contained (Independente)
Gera um executável maior que já contém todo o .NET Runtime. Roda em qualquer máquina Windows x64 sem instalar nada antes.

**Comando (PowerShell):**
```powershell
cd src/Presentation/DevTools.Presentation.Wpf
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/self_contained
```

*   **Resultado:** Um único arquivo `.exe` robusto.
*   **Tamanho:** ~60-80 MB.

---

## 📂 O que Distribuir?

Após o publish, você deve distribuir o conteúdo da pasta de saída (`publish/...`).

### Estrutura Típica de Distribuição:
```text
/DevTools_v1.0
  ├── DevTools.Presentation.Wpf.exe  (O Aplicativo Principal)
  ├── appsettings.json               (Configurações padrão, opcional)
  ├── Assets/                        (Ícones e recursos, geralmente embutidos no exe se SingleFile=true)
  └── README.txt                     (Instruções rápidas)
```

**Nota Importante:**
Se você usar a opção `PublishSingleFile=true`, a maioria das DLLs será empacotada dentro do `.exe`. No entanto, arquivos de configuração externos ou pastas que o aplicativo espera encontrar *junto* ao executável devem ser copiados manualmente se não estiverem embutidos.

---

## 📝 Check-list de Distribuição

1.  **Limpar Configurações Pessoais:**
    Certifique-se de que o código não contém caminhos hardcoded (ex: `C:\Users\Rodrigo\...`). O DevTools já usa `%APPDATA%` para configurações de usuário, o que é correto.

2.  **Ícone:**
    Verifique se o `DevTools.Presentation.Wpf.exe` está com o ícone correto (`app.ico`). Isso é definido no `.csproj`.

3.  **Testar em Ambiente Limpo:**
    Antes de distribuir, teste o `.exe` em uma máquina virtual ou sandbox (Windows Sandbox) para garantir que ele abre sem pedir DLLs faltantes.

4.  **Assinatura Digital (Opcional):**
    O Windows pode exibir o alerta "SmartScreen protegeu seu PC" para executáveis não assinados baixados da internet. Para uso interno/pessoal, basta clicar em "Mais informações" -> "Executar assim mesmo". Para distribuição pública profissional, seria necessário um certificado de assinatura de código (Code Signing Certificate).

---

## 🔄 Atualização

Como o DevTools não possui um sistema de *auto-update* embutido:
1.  Para atualizar, basta substituir o arquivo `.exe` antigo pelo novo.
2.  As configurações do usuário (em `%APPDATA%\DevTools`) **serão preservadas**, pois ficam separadas do executável.
