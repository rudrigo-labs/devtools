# Guia de Estilo e Modernização Visual (WPF Material Design)

Este documento descreve o padrão visual adotado para as telas de configuração do DevTools, utilizando a biblioteca **MaterialDesignInXaml**. Caso os créditos do assistente terminem, utilize este guia para manter a consistência.

## 1. Estrutura de Agrupamento (Cards)
Sempre agrupe campos relacionados dentro de um `materialDesign:Card`. Isso cria uma separação visual clara.

**Exemplo de Estrutura:**
```xml
<materialDesign:Card Padding="16" Margin="0,0,0,16" Background="#2D2D30">
    <StackPanel>
        <!-- Cabeçalho com Ícone -->
        <StackPanel Orientation="Horizontal" Margin="0,0,0,16">
            <materialDesign:PackIcon Kind="NOME_DO_ICONE" Foreground="{DynamicResource AccentColor}" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBlock Text="Título da Seção" FontSize="16" Foreground="White" FontWeight="SemiBold"/>
        </StackPanel>

        <!-- Campos aqui -->
    </StackPanel>
</materialDesign:Card>
```

## 2. Campos de Texto (TextBox)
Para manter a compatibilidade e evitar problemas de layout global, utilize o estilo padrão do projeto com um `Label` discreto acima do campo.

**Regras:**
- Use um `Label` com `Foreground="{DynamicResource SecondaryTextBrush}"` e `FontSize="11"`.
- O `TextBox` usará automaticamente o estilo `ModernTextBoxStyle` definido no tema.
- Use `Foreground="White"`.
- **NUNCA** use `MaterialDesignFloatingHintTextBox` globalmente, pois ele pode interferir em outras partes do sistema.

**Exemplo:**
```xml
<StackPanel Margin="0,16,0,0">
    <Label Content="Descrição do Campo" 
           Foreground="{DynamicResource SecondaryTextBrush}" 
           FontSize="11" 
           Margin="0,0,0,2"/>
    <TextBox Foreground="White"/>
</StackPanel>
```

## 3. Layout de Colunas (Grid)
Para campos curtos, use um `Grid` com duas colunas para otimizar o espaço.

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="20"/> <!-- Espaçador -->
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <TextBox Grid.Column="0" ... />
    <TextBox Grid.Column="2" ... />
</Grid>
```

## 4. Botões de Ação
- Botões principais (Salvar): Use `Style="{DynamicResource PrimaryButtonStyle}"`.
- Botões de perigo (Remover): Use `Background="Transparent" Foreground="#E81123" BorderThickness="0"`.

## 5. CheckBoxes
Utilize o estilo padrão do projeto, que já é customizado para o tema escuro.

```xml
<CheckBox Content="Opção" 
          Foreground="White" 
          Cursor="Hand" 
          Margin="0,10,0,0"/>
```
