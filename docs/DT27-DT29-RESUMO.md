# DT27 e DT29 — Resumo

## DT27 — Desempenho do carregamento inicial

Escopo revisado: medir a solução integrada após concluir as demais DTs da versão, antes de escolher otimizações. Considerar concluído o carregamento quando a primeira tela útil estiver pronta para interação. Separar download/inicialização do Blazor, autenticação e resposta da API/banco; comparar acesso sem cache, com cache e após inatividade da infraestrutura. Definir a meta a partir da medição inicial e comprovar as melhorias com comparação antes/depois nas mesmas condições.

Status: revisão de escopo concluída; medições e otimizações pendentes. Nenhuma melhoria de desempenho foi implementada ou medida neste commit.

## DT29 — Paleta do Admin

Implementada paleta azul para usuários autenticados com role Admin, inclusive fora das rotas /admin. Mantida a paleta verde para os demais perfis e para acesso anônimo. A seleção acompanha mudanças do estado de autenticação. Os temas claro e escuro continuam disponíveis. Acrescentada identificação textual Admin no cabeçalho.

Validação: build de Dima.Web concluído sem erros; validação visual no navegador pendente.

## DT28 — Sessão por inatividade

Revisão local de 29/09/2026 após conclusão da validação da DT26: estado mantido no SQL, sem polling de validade, atividade agrupada em 1 minuto por aba e prazos de 15 minutos/8 horas preservados. Ver DT28-EXPIRACAO-SESSAO.md para implementação e aceitação pendente no navegador.
