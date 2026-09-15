import { FormEvent, useEffect, useState } from 'react'

type User = {
  id: string
  userName: string
  email: string
  fullName: string
  isActive: boolean
  roles: string[]
}

type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  user: User
}

type CreateUserRequest = {
  userName: string
  email: string
  fullName: string
  password: string
  role: 'Administrador' | 'Bodeguero'
}

type RolePermissions = {
  role: 'Administrador' | 'Bodeguero'
  permissions: string[]
}

type Article = {
  id: string
  code: string
  name: string
  description: string
  isActive: boolean
  createdAtUtc: string
}

type CreateArticleRequest = {
  code: string
  name: string
  description: string
}

const permissionCatalog = [
  { key: 'Profile.Read', label: 'Consultar perfil autenticado' },
  { key: 'Users.Read', label: 'Listar usuarios' },
  { key: 'Users.Create', label: 'Crear usuarios' },
  { key: 'Articles.Read', label: 'Consultar articulos' },
  { key: 'Articles.Create', label: 'Crear articulos' },
]

const TOKEN_KEY = 'inventario.accessToken'
const API_URL = '/api'

async function request<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'No fue posible completar la solicitud.' }))
    throw new Error(error.message ?? 'No fue posible completar la solicitud.')
  }

  return response.json() as Promise<T>
}

function App() {
  const [token, setToken] = useState(() => sessionStorage.getItem(TOKEN_KEY))
  const [user, setUser] = useState<User | null>(null)
  const [users, setUsers] = useState<User[]>([])
  const [articles, setArticles] = useState<Article[]>([])
  const [activeView, setActiveView] = useState<'overview' | 'users' | 'articles' | 'permissions'>('overview')
  const [isLoading, setIsLoading] = useState(Boolean(token))
  const [error, setError] = useState('')
  const [rolePermissions, setRolePermissions] = useState<RolePermissions[]>([])

  useEffect(() => {
    if (!token) return

    request<User>('/auth/me', {}, token)
      .then(setUser)
      .catch(() => {
        sessionStorage.removeItem(TOKEN_KEY)
        setToken(null)
      })
      .finally(() => setIsLoading(false))
  }, [token])

  useEffect(() => {
    if (activeView !== 'users' || !token || !user?.roles.includes('Administrador')) return
    request<User[]>('/auth/users', {}, token).then(setUsers).catch((reason: Error) => setError(reason.message))
  }, [activeView, token, user])

  useEffect(() => {
    if (activeView !== 'articles' || !token) return
    request<Article[]>('/articles', {}, token).then(setArticles).catch((reason: Error) => setError(reason.message))
  }, [activeView, token])

  useEffect(() => {
    if (activeView !== 'permissions' || !token || !user?.roles.includes('Administrador')) return
    request<RolePermissions[]>('/permissions', {}, token).then(setRolePermissions).catch((reason: Error) => setError(reason.message))
  }, [activeView, token, user])

  function handleLogin(response: LoginResponse) {
    sessionStorage.setItem(TOKEN_KEY, response.accessToken)
    setToken(response.accessToken)
    setUser(response.user)
    setError('')
  }

  function logout() {
    sessionStorage.removeItem(TOKEN_KEY)
    setToken(null)
    setUser(null)
    setUsers([])
  }

  if (isLoading) return <div className="loading-screen">Cargando centro de control...</div>
  if (!token || !user) return <Login onLogin={handleLogin} error={error} setError={setError} />

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-mark"><span>NX</span><div><strong>NexoStock</strong><small>gestión operativa</small></div></div>
        <div className="workspace-label">ESPACIO DE TRABAJO</div>
        <nav className="nav-list">
          <button className={activeView === 'overview' ? 'nav-item active' : 'nav-item'} onClick={() => setActiveView('overview')}><span className="nav-icon">◈</span> Resumen</button>
          {user.roles.includes('Administrador') && <button className={activeView === 'users' ? 'nav-item active' : 'nav-item'} onClick={() => setActiveView('users')}><span className="nav-icon">◌</span> Usuarios</button>}
          {user.roles.includes('Administrador') && <button className={activeView === 'permissions' ? 'nav-item active' : 'nav-item'} onClick={() => setActiveView('permissions')}><span className="nav-icon">◇</span> Permisos</button>}
          <button className={activeView === 'articles' ? 'nav-item active' : 'nav-item'} onClick={() => { setError(''); setActiveView('articles') }}><span className="nav-icon">▦</span> Articulos</button>
        </nav>
        <div className="sidebar-footer"><span className="status-dot" /> API conectada <span className="api-port">:5027</span></div>
      </aside>
      <main className="main-content">
        <header className="topbar"><div><p className="eyebrow">Lunes, 14 de septiembre de 2026</p><h1>{activeView === 'overview' ? 'Resumen operativo' : activeView === 'users' ? 'Usuarios del sistema' : activeView === 'articles' ? 'Articulos del inventario' : 'Permisos por rol'}</h1></div><div className="profile-menu"><div className="avatar">{user.fullName.slice(0, 1)}</div><div><strong>{user.fullName}</strong><small>{user.roles.join(' · ')}</small></div><button className="logout-button" onClick={logout}>Salir</button></div></header>
        {error && <div className="alert">{error}</div>}
        {activeView === 'overview' ? <Overview user={user} /> : activeView === 'users' ? <Users users={users} token={token} onUserCreated={(created) => setUsers((current) => [...current, created].sort((left, right) => left.userName.localeCompare(right.userName)))} /> : activeView === 'articles' ? <Articles articles={articles} token={token} onArticleCreated={(created) => setArticles((current) => [...current, created].sort((left, right) => left.code.localeCompare(right.code)))} /> : <Permissions permissions={rolePermissions} token={token} onSaved={setRolePermissions} />}
      </main>
    </div>
  )
}

function Login({ onLogin, error, setError }: { onLogin: (response: LoginResponse) => void; error: string; setError: (value: string) => void }) {
  const [identifier, setIdentifier] = useState('admin@inventario.test')
  const [password, setPassword] = useState('Admin123!Test')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setIsSubmitting(true)
    setError('')
    try {
      onLogin(await request<LoginResponse>('/auth/login', { method: 'POST', body: JSON.stringify({ userNameOrEmail: identifier, password }) }))
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Credenciales invalidas.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return <div className="login-page"><div className="login-decoration"><div className="orbit orbit-one" /><div className="orbit orbit-two" /><div className="login-signal">NEXOSTOCK<br /><span>CONTROL ROOM</span></div></div><section className="login-panel"><div className="brand-mark dark"><span>NX</span><div><strong>NexoStock</strong><small>gestión operativa</small></div></div><div className="login-copy"><p className="eyebrow">Acceso seguro</p><h1>Tu operación, en un solo lugar.</h1><p>Administra usuarios y mantén la visibilidad de tu sistema desde un espacio diseñado para decidir rápido.</p></div><form onSubmit={submit}><label>Correo o usuario<input value={identifier} onChange={(event) => setIdentifier(event.target.value)} autoComplete="username" required /></label><label>Contraseña<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" required /></label>{error && <div className="form-error">{error}</div>}<button className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Validando...' : 'Entrar al sistema'} <span>→</span></button></form><small className="login-note">Sesión protegida con autenticación JWT</small></section></div>
}

function Overview({ user }: { user: User }) {
  return <section className="content-grid"><div className="welcome-banner"><div><p className="eyebrow">Panel principal</p><h2>Buenos días, {user.fullName.split(' ')[0]}.</h2><p>El núcleo de autenticación está activo. Desde aquí puedes supervisar el acceso al sistema.</p></div><div className="banner-stamp">14<br /><span>SEP</span></div></div><div className="metrics"><article className="metric-card accent"><span className="metric-label">Estado de sesión</span><strong>Activa</strong><small>Token válido y protegido</small></article><article className="metric-card"><span className="metric-label">Tu rol</span><strong>{user.roles[0] ?? 'Usuario'}</strong><small>Permisos asignados</small></article><article className="metric-card"><span className="metric-label">Acceso</span><strong>Seguro</strong><small>API conectada vía JWT</small></article></div><div className="activity-panel"><div className="section-heading"><div><p className="eyebrow">Actividad reciente</p><h3>Todo en orden</h3></div><span className="live-label"><i /> EN VIVO</span></div><div className="activity-row"><span className="activity-icon">✓</span><div><strong>Sesión iniciada correctamente</strong><small>Autenticación validada contra Inventario API</small></div><time>Ahora</time></div><div className="activity-row"><span className="activity-icon muted">↗</span><div><strong>Perfil cargado</strong><small>{user.email}</small></div><time>Ahora</time></div></div></section>
}

function Articles({ articles, token, onArticleCreated }: { articles: Article[]; token: string; onArticleCreated: (article: Article) => void }) {
  const [form, setForm] = useState<CreateArticleRequest>({ code: '', name: '', description: '' })
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [formError, setFormError] = useState('')

  async function createArticle(event: FormEvent) {
    event.preventDefault()
    setIsSubmitting(true)
    setFormError('')
    try {
      const created = await request<Article>('/articles', { method: 'POST', body: JSON.stringify(form) }, token)
      onArticleCreated(created)
      setForm({ code: '', name: '', description: '' })
    } catch (reason) {
      setFormError(reason instanceof Error ? reason.message : 'No fue posible crear el articulo.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return <section className="content-grid articles-view"><div className="users-toolbar"><div><p className="eyebrow">Catalogo base</p><h2>Articulos disponibles</h2></div><span className="count-badge">{articles.length} registrados</span></div><form className="create-user-form" onSubmit={createArticle}><div className="form-heading"><div><p className="eyebrow">Nuevo articulo</p><h3>Datos basicos</h3></div><span className="role-tag">Codigo unico</span></div><div className="form-fields"><label>Codigo<input value={form.code} onChange={(event) => setForm({ ...form, code: event.target.value })} placeholder="ART-001" maxLength={50} required /></label><label>Nombre<input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} maxLength={150} required /></label><label className="wide-field">Descripcion<input value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={500} /></label></div>{formError && <div className="form-error">{formError}</div>}<button className="primary-button create-button" disabled={isSubmitting}>{isSubmitting ? 'Creando...' : 'Crear articulo'} <span>+</span></button></form><div className="table-wrap"><table><thead><tr><th>Codigo</th><th>Articulo</th><th>Descripcion</th><th>Estado</th></tr></thead><tbody>{articles.map((article) => <tr key={article.id}><td><span className="role-tag">{article.code}</span></td><td><div className="table-user"><span className="small-avatar">{article.name.slice(0, 1)}</span><div><strong>{article.name}</strong><small>Alta {new Date(article.createdAtUtc).toLocaleDateString()}</small></div></div></td><td>{article.description || 'Sin descripcion'}</td><td><span className="state"><i /> {article.isActive ? 'Activo' : 'Inactivo'}</span></td></tr>)}</tbody></table></div></section>
}

function Permissions({ permissions, token, onSaved }: { permissions: RolePermissions[]; token: string; onSaved: (value: RolePermissions[]) => void }) {
  const [selectedRole, setSelectedRole] = useState<RolePermissions['role']>('Administrador')
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([])
  const [isSaving, setIsSaving] = useState(false)
  const [message, setMessage] = useState('')
  const current = permissions.find((item) => item.role === selectedRole)

  useEffect(() => setSelectedPermissions(current?.permissions ?? []), [current])

  function togglePermission(permission: string) {
    setSelectedPermissions((currentPermissions) => currentPermissions.includes(permission)
      ? currentPermissions.filter((item) => item !== permission)
      : [...currentPermissions, permission])
  }

  async function save() {
    setIsSaving(true)
    setMessage('')
    try {
      const updated = await request<RolePermissions>(`/permissions/${selectedRole}`, { method: 'PUT', body: JSON.stringify({ permissions: selectedPermissions }) }, token)
      onSaved(permissions.map((item) => item.role === updated.role ? updated : item))
      setMessage('Permisos guardados correctamente.')
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'No fue posible guardar los permisos.')
    } finally {
      setIsSaving(false)
    }
  }

  return <section className="content-grid permissions-view"><div className="users-toolbar"><div><p className="eyebrow">Control de acceso</p><h2>Permisos por rol</h2></div><span className="role-tag">Solo administradores</span></div><div className="permission-panel"><label>Rol<select value={selectedRole} onChange={(event) => setSelectedRole(event.target.value as RolePermissions['role'])}><option value="Administrador">Administrador</option><option value="Bodeguero">Bodeguero</option></select></label><div className="permission-list">{permissionCatalog.map((permission) => <label className="permission-item" key={permission.key}><input type="checkbox" checked={selectedPermissions.includes(permission.key)} onChange={() => togglePermission(permission.key)} /><span><strong>{permission.key}</strong><small>{permission.label}</small></span></label>)}</div>{message && <div className={message.includes('correctamente') ? 'form-success' : 'form-error'}>{message}</div>}<button className="primary-button create-button" onClick={save} disabled={isSaving}>{isSaving ? 'Guardando...' : 'Guardar permisos'} <span>✓</span></button></div></section>
}

function Users({ users, token, onUserCreated }: { users: User[]; token: string; onUserCreated: (user: User) => void }) {
  const [form, setForm] = useState<CreateUserRequest>({ userName: '', email: '', fullName: '', password: '', role: 'Bodeguero' })
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [formError, setFormError] = useState('')

  async function createUser(event: FormEvent) {
    event.preventDefault()
    setIsSubmitting(true)
    setFormError('')

    try {
      const created = await request<User>('/auth/users', { method: 'POST', body: JSON.stringify(form) }, token)
      onUserCreated(created)
      setForm({ userName: '', email: '', fullName: '', password: '', role: 'Bodeguero' })
    } catch (reason) {
      setFormError(reason instanceof Error ? reason.message : 'No fue posible crear el usuario.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return <section className="content-grid users-view"><div className="users-toolbar"><div><p className="eyebrow">Directorio</p><h2>Personas con acceso</h2></div><span className="count-badge">{users.length} registrados</span></div><form className="create-user-form" onSubmit={createUser}><div className="form-heading"><div><p className="eyebrow">Nuevo acceso</p><h3>Crear usuario</h3></div><span className="role-tag">Solo administradores</span></div><div className="form-fields"><label>Nombre completo<input value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} autoComplete="name" required /></label><label>Usuario<input value={form.userName} onChange={(event) => setForm({ ...form, userName: event.target.value })} autoComplete="username" required /></label><label>Correo<input type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} autoComplete="email" required /></label><label>Contraseña<input type="password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} autoComplete="new-password" minLength={12} required /></label><label>Rol<select value={form.role} onChange={(event) => setForm({ ...form, role: event.target.value as CreateUserRequest['role'] })}><option value="Bodeguero">Bodeguero</option><option value="Administrador">Administrador</option></select></label></div>{formError && <div className="form-error">{formError}</div>}<button className="primary-button create-button" disabled={isSubmitting}>{isSubmitting ? 'Creando...' : 'Crear usuario'} <span>+</span></button></form><div className="table-wrap"><table><thead><tr><th>Usuario</th><th>Correo</th><th>Rol</th><th>Estado</th></tr></thead><tbody>{users.map((item) => <tr key={item.id}><td><div className="table-user"><span className="small-avatar">{item.fullName.slice(0, 1)}</span><div><strong>{item.fullName}</strong><small>{item.userName}</small></div></div></td><td>{item.email}</td><td><span className="role-tag">{item.roles[0] ?? 'Sin rol'}</span></td><td><span className="state"><i /> {item.isActive ? 'Activo' : 'Inactivo'}</span></td></tr>)}</tbody></table></div></section>
}

export default App
