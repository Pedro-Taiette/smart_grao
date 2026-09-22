import { Alert, AlertTitle, Button, Stack } from '@mui/material';
import { useNavigate } from 'react-router-dom';

export interface SetupGap {
  /** Chave estável — a ordem da lista muda conforme o que falta. */
  id: string;
  title: string;
  description: string;
  actionLabel: string;
  to: string;
}

/**
 * O que falta cadastrar antes de a primeira vistoria ser possivel.
 *
 * O sistema tem uma ordem obrigatoria — fazenda, talhao, cultivo, equipe, protocolo, pontos — e
 * antes disso ela so aparecia como um botao desabilitado sem explicacao, ou como um erro depois de
 * preencher um formulario inteiro. Aqui a ordem vira a propria tela: uma pendencia por vez, dizendo
 * por que ela bloqueia e levando direto ao lugar de resolver.
 */
export function PendingSetup({ gaps }: { gaps: SetupGap[] }) {
  const navigate = useNavigate();

  if (gaps.length === 0) return null;

  return (
    <Stack spacing={2}>
      {gaps.map((gap) => (
        <Alert
          key={gap.id}
          severity="info"
          action={
            <Button color="inherit" size="small" onClick={() => navigate(gap.to)}>
              {gap.actionLabel}
            </Button>
          }
        >
          <AlertTitle>{gap.title}</AlertTitle>
          {gap.description}
        </Alert>
      ))}
    </Stack>
  );
}
