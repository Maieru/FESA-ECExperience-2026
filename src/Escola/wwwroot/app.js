const form = document.querySelector('#formulario');
const mensagem = document.querySelector('#mensagem');
const salvar = document.querySelector('#salvar');
const cancelar = document.querySelector('#cancelar');
const cursos = Object.fromEntries([...form.elements.curso.options].map(option => [option.value, option.text]));
let editando = null;
let ocupado = false;

function avisar(texto, erro = false) {
  mensagem.textContent = texto;
  mensagem.className = erro ? 'erro' : '';
}

async function requisitar(url, options = {}) {
  const resposta = await fetch(url, options);
  if (!resposta.ok) {
    const corpo = await resposta.json().catch(() => ({}));
    throw new Error(corpo.erro || 'Não foi possível concluir. Confira os dados e tente novamente.');
  }
  return resposta.status === 204 ? null : resposta.json();
}

function limpar() {
  editando = null;
  form.reset();
  document.querySelector('#titulo-formulario').textContent = 'Novo aluno';
  salvar.textContent = 'Cadastrar aluno';
  cancelar.hidden = true;
}

function editar(aluno) {
  if (ocupado) return;
  editando = aluno.id;
  for (const campo of ['ra', 'nome', 'dataNascimento', 'dataEntrada', 'curso']) form.elements[campo].value = aluno[campo];
  document.querySelector('#titulo-formulario').textContent = 'Editar aluno';
  salvar.textContent = 'Salvar alterações';
  cancelar.hidden = false;
  form.elements.ra.focus();
}

function dataBR(data) { return data.split('-').reverse().join('/'); }

async function listar() {
  const alunos = await requisitar('/api/alunos');
  const corpo = document.querySelector('#alunos');
  corpo.replaceChildren();
  document.querySelector('#contador').textContent = alunos.length;
  document.querySelector('#tabela').hidden = alunos.length === 0;
  document.querySelector('#vazio').hidden = alunos.length > 0;
  document.querySelector('#vazio').textContent = 'Nenhum aluno cadastrado. Use o formulário para cadastrar o primeiro.';
  for (const aluno of alunos) {
    const linha = corpo.insertRow();
    const identidade = linha.insertCell();
    const nome = document.createElement('strong');
    nome.textContent = aluno.nome;
    const ra = document.createElement('small');
    ra.textContent = `RA ${aluno.ra}`;
    identidade.append(nome, ra);
    for (const valor of [cursos[aluno.curso], dataBR(aluno.dataNascimento), dataBR(aluno.dataEntrada)]) linha.insertCell().textContent = valor;
    const acoes = linha.insertCell();
    for (const [rotulo, executar] of [['Editar', () => editar(aluno)], ['Deletar', () => deletar(aluno)]]) {
      const botao = document.createElement('button');
      botao.type = 'button';
      botao.textContent = rotulo;
      botao.setAttribute('aria-label', `${rotulo} ${aluno.nome}`);
      botao.onclick = executar;
      acoes.append(botao);
    }
  }
}

async function executarMutacao(acao, sucesso) {
  if (ocupado) return;
  ocupado = true;
  salvar.disabled = cancelar.disabled = true;
  try {
    await acao();
    avisar(sucesso);
    try { await listar(); }
    catch { avisar(`${sucesso} Não foi possível atualizar a lista. Recarregue a página.`, true); }
  } catch (erro) { avisar(erro.message, true); }
  finally { ocupado = false; salvar.disabled = cancelar.disabled = false; }
}

async function deletar(aluno) {
  if (ocupado || !confirm(`Deletar ${aluno.nome} (RA ${aluno.ra})?`)) return;
  await executarMutacao(async () => {
    await requisitar(`/api/alunos/${aluno.id}`, { method: 'DELETE' });
    if (editando === aluno.id) limpar();
  }, 'Aluno deletado.');
}

form.addEventListener('submit', async event => {
  event.preventDefault();
  const dados = Object.fromEntries(new FormData(form));
  await executarMutacao(async () => {
    await requisitar(editando ? `/api/alunos/${editando}` : '/api/alunos', {
      method: editando ? 'PUT' : 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(dados)
    });
    limpar();
  }, editando ? 'Aluno atualizado.' : 'Aluno cadastrado.');
});
cancelar.onclick = limpar;
listar().catch(() => {
  document.querySelector('#vazio').textContent = 'Lista indisponível. Recarregue a página para tentar novamente.';
  avisar('Não foi possível carregar os alunos. Verifique se o servidor está disponível.', true);
});
