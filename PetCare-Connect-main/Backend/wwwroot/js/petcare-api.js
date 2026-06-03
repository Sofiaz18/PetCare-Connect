const API_BASE = 'http://localhost:5000/api';

// ======================================================
// SESSÃO (localStorage — persiste entre abas)
// ======================================================

function petcareSetSession(token, usuarioId, nome, login, perfil) {
    localStorage.setItem('petcare_token',     token);
    localStorage.setItem('petcare_usuarioId', usuarioId);
    localStorage.setItem('petcare_nome',      nome);
    localStorage.setItem('petcare_login',     login);
    localStorage.setItem('petcare_perfil',    perfil);
}

function petcareGetSession() {
    return {
        token:     localStorage.getItem('petcare_token'),
        usuarioId: localStorage.getItem('petcare_usuarioId'),
        nome:      localStorage.getItem('petcare_nome'),
        login:     localStorage.getItem('petcare_login'),
        perfil:    localStorage.getItem('petcare_perfil')
    };
}

function petcareGetToken()  { return localStorage.getItem('petcare_token');  }
function petcareGetPerfil() { return localStorage.getItem('petcare_perfil'); }
function petcareGetUserId() { return parseInt(localStorage.getItem('petcare_usuarioId') || '0'); }

// ======================================================
// AUTH GUARD
// ======================================================

function petcareRequireAuth(perfilEsperado) {
    const token  = petcareGetToken();
    const perfil = petcareGetPerfil();
    if (!token)                                      { window.location.href = 'index.html'; return false; }
    if (perfilEsperado && perfil !== perfilEsperado) { window.location.href = 'index.html'; return false; }
    return true;
}

// ======================================================
// LOGOUT
// ======================================================

function petcareLogout() {
    ['petcare_token','petcare_usuarioId','petcare_nome',
     'petcare_login','petcare_perfil'].forEach(k => localStorage.removeItem(k));
    window.location.href = 'index.html';
}

// ======================================================
// FETCH HELPER CENTRAL
// ======================================================

async function apiCall(method, endpoint, body = null) {
    const headers = { 'Content-Type': 'application/json' };
    const token   = petcareGetToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const opts = { method, headers };
    if (body !== null) opts.body = JSON.stringify(body);

    try {
        const res = await fetch(`${API_BASE}${endpoint}`, opts);

        if (res.status === 401) {
            // Token expirado — volta para o login
            petcareLogout();
            return null;
        }

        return await res.json();
    } catch (err) {
        console.error('ERRO API:', err);
        return { sucesso: false, mensagem: 'Servidor offline ou inacessível.' };
    }
}

// ======================================================
// AUTH
// ======================================================

async function petcareLogin(login, senha, perfil) {
    return await apiCall('POST', '/Auth/login', { login, senha, perfil });
}

async function petcareCadastro(nome, login, email, senha) {
    return await apiCall('POST', '/Auth/cadastro', { nome, login, email, senha });
}

async function petcareRecuperarSenha(login, email) {
    return await apiCall('POST', '/Auth/recuperar-senha', { login, email });
}

async function petcareAlterarSenha(login, novaSenha) {
    return await apiCall('POST', '/Auth/nova-senha', { login, novaSenha });
}

// ======================================================
// USUÁRIOS
// ======================================================

async function petcareListarVeterinarios() {
    return await apiCall('GET', '/Usuarios/veterinarios');
}

async function petcareListarUsuarios(perfil = null) {
    const q = perfil ? `?perfil=${perfil}` : '';
    return await apiCall('GET', `/Usuarios${q}`);
}

async function petcareCriarUsuario(dados) {
    return await apiCall('POST', '/Usuarios', dados);
}

async function petcareToggleAtivo(id, ativo) {
    return await apiCall('PATCH', `/Usuarios/${id}/ativo`, ativo);
}

// ======================================================
// PETS
// ======================================================

async function petcareListarPets() {
    return await apiCall('GET', '/Pets');
}

async function petcareBuscarPet(id) {
    return await apiCall('GET', `/Pets/${id}`);
}

async function petcareCriarPet(nome, tipo, idade, peso, tutorId) {

    return await apiCall(
        'POST',
        '/Pets',
        {
            nome,
            tipo,
            idade,
            peso,
            tutorId
        }
    );
}

async function petcareAtualizarPet(id, idade, peso) {
    return await apiCall('PATCH', `/Pets/${id}`, { idade, peso });
}

async function petcareRemoverPet(id) {
    return await apiCall('DELETE', `/Pets/${id}`);
}

// ======================================================
// CONSULTAS
// ======================================================

async function petcareListarConsultas(petId = null, vetId = null) {
    const params = [];
    if (petId) params.push(`petId=${petId}`);
    if (vetId) params.push(`vetId=${vetId}`);
    const q = params.length ? '?' + params.join('&') : '';
    return await apiCall('GET', `/Consultas${q}`);
}

async function petcareAgendarConsulta(petId, veterinarioId, data, hora) {
    // data: dd/MM/yyyy, hora: HH:mm
    return await apiCall('POST', '/Consultas', { petId, veterinarioId, data, hora });
}

async function petcareAtualizarStatusConsulta(id, status, obs = null) {
    const params = [`status=${encodeURIComponent(status)}`];
    if (obs) params.push(`obs=${encodeURIComponent(obs)}`);
    return await apiCall('PATCH', `/Consultas/${id}/status?${params.join('&')}`);
}

// ======================================================
// VACINAS
// ======================================================

async function petcareListarVacinas(petId) {
    return await apiCall('GET', `/Vacinas/${petId}`);
}

async function petcareCriarVacina(petId, nome, dataAplicacao, observacao, aplicada) {
    return await apiCall('POST', '/Vacinas', { petId, nome, dataAplicacao, observacao, aplicada });
}

async function petcareAplicarVacina(id, data, obs = null) {
    const params = [`data=${encodeURIComponent(data)}`];
    if (obs) params.push(`obs=${encodeURIComponent(obs)}`);
    return await apiCall('PATCH', `/Vacinas/${id}/aplicar?${params.join('&')}`);
}

async function petcareAtualizarVacina(id, nome, dataAplicacao, observacao) {
    return await apiCall('PATCH', `/Vacinas/${id}`, {
        nome,
        dataAplicacao,
        observacao
    });
}

async function petcareExcluirVacina(id) {
    return await apiCall('DELETE', `/Vacinas/${id}`);
}

// ======================================================
// HISTÓRICO
// ======================================================

async function petcareListarHistorico(petId) {
    return await apiCall('GET', `/Historico/${petId}`);
}

async function petcareAdicionarHistorico(petId, descricao, dataEvento = null) {
    return await apiCall('POST', '/Historico', { petId, descricao, dataEvento });
}
async function petcareExcluirHistorico(id) {
    return await apiCall('DELETE', `/Historico/${id}`);
}

// ======================================================
// AVISOS
// ======================================================

async function petcareListarAvisos() {
    return await apiCall('GET', '/Avisos');
}

async function petcareCriarAviso(titulo, texto, tipo, tutorId = null, petId = null) {
    return await apiCall('POST', '/Avisos', { titulo, texto, tipo, tutorId, petId });
}

async function petcareMarcarAvisoLido(id) {
    return await apiCall('POST', `/Avisos/${id}/lido`);
}

async function enviarAvisoTutor() {

    const petId =
        parseInt(
            document.getElementById('avisoPetTutor')?.value
        );

    const titulo =
        document.getElementById('avisoTutorTitulo')
            ?.value
            .trim();

    const texto =
        document.getElementById('avisoTutorTexto')
            ?.value
            .trim();

    if (!petId || !titulo || !texto) {

        mostrarMensagem(
            'Preencha pet, título e mensagem.'
        );

        return;
    }

    const result =
        await petcareCriarAviso(
            titulo,
            texto,
            'manual',
            null,
            petId
        );

    if (!result?.sucesso) {

        mostrarMensagem(
            result?.mensagem ||
            'Erro ao enviar aviso.'
        );

        return;
    }

    mostrarMensagem('Aviso enviado!');

    document.getElementById(
        'avisoTutorTitulo'
    ).value = '';

    document.getElementById(
        'avisoTutorTexto'
    ).value = '';

    await carregarAvisosTutorEnviados();
    await carregarAvisos();
}
// ======================================================
// RELATÓRIOS
// ======================================================

async function petcareListarRelatorios(petId = null) {
    const q = petId ? `?petId=${petId}` : '';
    return await apiCall('GET', `/Relatorios${q}`);
}

async function petcareSalvarRelatorio(petId, texto) {
    return await apiCall('POST', '/Relatorios', { petId, texto });
}

async function petcareAtualizarRelatorio(petId, texto) {
    return await apiCall('PATCH', `/Relatorios/${petId}`, {
        petId,
        texto
    });
}

async function petcareExcluirRelatorio(petId) {
    return await apiCall('DELETE', `/Relatorios/${petId}`);
}

// ======================================================
// ADMIN
// ======================================================

async function petcareEstatisticas() {
    return await apiCall('GET', '/Admin/estatisticas');
}

// ======================================================
// UTILITÁRIOS DE DATA
// ======================================================

// "yyyy-MM-dd" → "dd/MM/yyyy"
function petcareBrDate(isoDate) {
    if (!isoDate) return '';
    const [y, m, d] = isoDate.split('-');
    return `${d}/${m}/${y}`;
}

// "dd/MM/yyyy" → "yyyy-MM-dd"
function petcareIsoDate(brDate) {
    if (!brDate) return '';
    const [d, m, y] = brDate.split('/');
    return `${y}-${m}-${d}`;
}

// ======================================================
// HORÁRIOS DISPONÍVEIS (8h–17h, slots de 30 min)
// ======================================================
function petcareHorariosDisponiveis() {
    const slots = [];
    for (let h = 8; h <= 17; h++) {
        slots.push(`${String(h).padStart(2,'0')}:00`);
        if (h < 17) slots.push(`${String(h).padStart(2,'0')}:30`);
    }
    return slots;
}

// ======================================================
// NAVEGAÇÃO PARA RECUPERAÇÃO DE SENHA
// ======================================================
function abrirRecuperacaoSenha() {
    window.location.href = 'recuperar-senha.html';
}

async function carregarAvisosTutorEnviados() {

    const result = await petcareListarAvisos();

    const lista =
        document.getElementById(
            'listaAvisosTutorEnviados'
        );

    if (!lista) return;

    const avisos =
        result?.dados || [];

    lista.innerHTML = '';

    if (!avisos.length) {

        lista.innerHTML = `
            <div class="empty-state">
                Nenhum aviso enviado.
            </div>
        `;

        return;
    }

    lista.innerHTML =
        avisos.map(a => `

            <div class="card">

                <div class="card-header">

                    <h4 style="margin:0">
                        📢 ${a.titulo || 'Aviso'}
                    </h4>

                    <span class="chip">
                        ${a.lido ? '✓ Lido' : '⏳ Pendente'}
                    </span>

                </div>

                <p style="margin:12px 0">
                    ${a.texto || ''}
                </p>

                <div style="
                    display:flex;
                    gap:8px;
                    flex-wrap:wrap;
                    margin-bottom:12px;
                ">

                    <span class="chip">
                        ${a.tipo || 'aviso'}
                    </span>

                    <span class="chip">
                        Pet #${a.petId}
                    </span>

                </div>

                <small style="opacity:.7">
                    ${petcareFormatarData(a.criadoEm)}
                </small>

            </div>

        `).join('');
}

function petcareFormatarData(data){

    if(!data) return '-';

    return new Date(data)
        .toLocaleString('pt-BR');
}

function formatarTelefone(input){

    let valor = input.value.replace(/\D/g,'');

    valor = valor.replace(/^(\d{2})(\d)/g,'($1) $2');
    valor = valor.replace(/(\d{5})(\d)/,'$1-$2');

    input.value = valor;
}

async function petcareAtualizarUsuario(id, dados){

    return await apiCall(
        'PUT',
        `/Usuarios/${id}`,
        dados
    );
}

async function petcareExcluirUsuario(id){

    return await apiCall(
        'DELETE',
        `/Usuarios/${id}`
    );
}

async function petcareBuscarUsuario(id){

    return await apiCall(
        'GET',
        `/Usuarios/${id}`
    );
}

async function petcareBuscarUsuario(id){

    return await apiCall(
        'GET',
        `/Usuarios/${id}`
    );
}

async function petcareCriarConsulta(
    petId,
    veterinarioId,
    data,
    hora
) {

    return await apiCall(
        'POST',
        '/Consultas',
        {
            petId,
            veterinarioId,
            data,
            hora
        }
    );
}

async function petcareEstatisticasComAvisos() {

    const stats = await petcareEstatisticas();

    const avisos = await petcareListarAvisos();

    if (!stats.dados) {
        stats.dados = {};
    }

    stats.dados.Avisos =
        (avisos?.dados || []).length;

    return stats;
}