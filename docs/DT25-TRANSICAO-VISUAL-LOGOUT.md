# DT-25 — Transição visual durante logout

Estado: implementada no frontend; validação visual autenticada pendente.

## Implementação

- O MudThemeProvider vive em App e permanece montado ao trocar de layout.
  ThemeState compartilha a escolha claro/escuro entre MainLayout, saída e login.
  A preferência do sistema é consultada uma vez ao iniciar a aplicação.
- /sair renderiza o estado de processamento antes de chamar a API.
- Sucesso é exibido somente após LogoutAsync concluir com resposta HTTP de sucesso.
  O estado de autenticação local é então limpo e publicado sem consultas adicionais
  a /me e /roles.
- Falha mantém a confirmação de saída oculta e oferece tentativa novamente.
  Chamadas simultâneas são bloqueadas enquanto a operação está em andamento.
- O botão após sucesso aponta diretamente para /login, evitando passar pelo
  dashboard protegido e seu redirecionamento.
- A imagem tem marcação válida e a atualização de estado usa aria-live.

## Validação executada

`dotnet build Dima.Web/Dima.Web.csproj --no-restore`: sucesso, zero erros.
Os cinco avisos estão em páginas de Admin, Transactions e Checkout fora desta DT.

## Aceitação visual pendente

Em uma sessão autenticada, repetir nos temas claro e escuro:

1. Sair e confirmar que o tema permanece estável entre dashboard, saída e login.
2. Com rede lenta, observar processamento sem confirmação prematura de sucesso.
3. Simular falha do endpoint: observar erro, sem sucesso nem navegação automática;
   restaurar a rede e tentar novamente.
4. Após sucesso, abrir /login e autenticar novamente; confirmar que não há passagem
   visual pelo dashboard ao clicar no botão de login.
5. Abrir /sair sem sessão e confirmar que o endpoint encerra a operação normalmente.

A compilação não comprova ausência de flicker no navegador nem implantação.
O backend e a política de revogação/expiração permanecem no escopo das DT-13/DT-28.
