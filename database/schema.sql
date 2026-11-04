CREATE TABLE roles (
    id_rol INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(30) NOT NULL UNIQUE,
    activo TINYINT(1) NOT NULL DEFAULT 1
);

CREATE TABLE personas (
    id_persona       INT AUTO_INCREMENT PRIMARY KEY,
    nombre           VARCHAR(80) NOT NULL,
    apellido         VARCHAR(80) NOT NULL,
    dni              VARCHAR(15) NOT NULL UNIQUE,
    sexo             ENUM('femenino','masculino','no_binario','otro','prefiero_no_decir') NOT NULL,
    fecha_nacimiento DATE NOT NULL,
    telefono         VARCHAR(30) NULL,
    domicilio        VARCHAR(200) NULL,
    email            VARCHAR(120) NOT NULL UNIQUE,
    username         VARCHAR(60) NOT NULL UNIQUE,
    password_hash    VARCHAR(255) NOT NULL,
    activo           TINYINT(1) NOT NULL DEFAULT 1,
    fecha_registro   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE personas_roles (
    id_persona INT NOT NULL,
    id_rol     INT NOT NULL,
    activo     TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id_persona, id_rol),
    CONSTRAINT fk_personas_roles_persona FOREIGN KEY (id_persona) REFERENCES personas(id_persona),
    CONSTRAINT fk_personas_roles_rol     FOREIGN KEY (id_rol) REFERENCES roles(id_rol)
);

CREATE TABLE carreras (
    id_carrera      INT AUTO_INCREMENT PRIMARY KEY,
    nombre          VARCHAR(120) NOT NULL UNIQUE,
    tipo            ENUM('curso','pregrado','grado','postgrado') NOT NULL DEFAULT 'grado',
    titulo_otorgado VARCHAR(120) NOT NULL,
    duracion_años   SMALLINT NOT NULL,
    activa          TINYINT(1) NOT NULL DEFAULT 1
);

CREATE TABLE inscripciones_carrera (
    id_inscripcion_carrera   INT AUTO_INCREMENT PRIMARY KEY,
    id_persona               INT NOT NULL,
    id_carrera               INT NOT NULL,
    fecha_inscripcion        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    año_ultima_reinscripcion SMALLINT NULL,
    fecha_reinscripcion      DATETIME NULL,
    estado                   ENUM('activa','inactiva','graduado') NOT NULL DEFAULT 'activa',
    UNIQUE KEY uq_inscripcion_carrera (id_persona, id_carrera),
    CONSTRAINT fk_icarrera_persona FOREIGN KEY (id_persona) REFERENCES personas(id_persona),
    CONSTRAINT fk_icarrera_carrera FOREIGN KEY (id_carrera) REFERENCES carreras(id_carrera)
);

CREATE TABLE materias (
    id_materia          INT AUTO_INCREMENT PRIMARY KEY,
    id_carrera          INT NOT NULL,
    codigo              VARCHAR(20) NOT NULL,
    nombre              VARCHAR(120) NOT NULL,
    año_en_carrera      SMALLINT NOT NULL,
    regimen_cursada     ENUM('cuatrimestral','anual') NOT NULL,
    carga_horaria_total INT NOT NULL,
    activa              TINYINT(1) NOT NULL DEFAULT 1,
    UNIQUE KEY uq_materia_carrera_codigo (id_carrera, codigo),
    CONSTRAINT fk_materia_carrera FOREIGN KEY (id_carrera) REFERENCES carreras(id_carrera)
);

CREATE TABLE correlatividades (
    id_correlatividad    INT AUTO_INCREMENT PRIMARY KEY,
    id_materia           INT NOT NULL,
    id_materia_requisito INT NOT NULL,
    tipo_requisito       ENUM('regularizada','aprobada') NOT NULL DEFAULT 'regularizada',
    UNIQUE KEY uq_correlatividad (id_materia, id_materia_requisito),
    CONSTRAINT fk_corr_materia   FOREIGN KEY (id_materia) REFERENCES materias(id_materia),
    CONSTRAINT fk_corr_requisito FOREIGN KEY (id_materia_requisito) REFERENCES materias(id_materia)
);

CREATE TABLE asignaciones_docentes (
    id_asignacion INT AUTO_INCREMENT PRIMARY KEY,
    id_materia    INT NOT NULL,
    id_persona    INT NOT NULL,
    id_comision   INT NULL,
    cargo         ENUM('profesor_titular','profesor_asociado','profesor_adjunto','jefe_trabajos_practicos','ayudante_primera','ayudante_segunda') NOT NULL,
    fecha_desde   DATE NOT NULL,
    fecha_hasta   DATE NULL,
    activo        TINYINT(1) NOT NULL DEFAULT 1,
    UNIQUE KEY uq_asignacion_docente (id_materia, id_persona, id_comision, cargo, fecha_desde),
    CONSTRAINT fk_asigdoc_materia FOREIGN KEY (id_materia) REFERENCES materias(id_materia),
    CONSTRAINT fk_asigdoc_persona FOREIGN KEY (id_persona) REFERENCES personas(id_persona)
);

CREATE TABLE periodos (
    id_periodo                     INT AUTO_INCREMENT PRIMARY KEY,
    año                            SMALLINT NOT NULL,
    tipo                           ENUM('primer_cuatrimestre','segundo_cuatrimestre','anual') NOT NULL DEFAULT 'primer_cuatrimestre',
    fecha_inicio                   DATE NOT NULL,
    fecha_fin                      DATE NOT NULL,
    inscripcion_comisiones_abierta TINYINT(1) NOT NULL DEFAULT 0,
    inscripcion_finales_abierta    TINYINT(1) NOT NULL DEFAULT 0,
    activo                         TINYINT(1) NOT NULL DEFAULT 1,
    UNIQUE KEY uq_periodo (año, tipo)
);

CREATE TABLE comisiones (
    id_comision         INT AUTO_INCREMENT PRIMARY KEY,
    id_materia          INT NOT NULL,
    id_periodo          INT NOT NULL,
    codigo              VARCHAR(10) NOT NULL,
    turno               ENUM('mañana','tarde','noche') NOT NULL,
    modalidad_dictado   ENUM('presencial','virtual','hibrida') NOT NULL DEFAULT 'presencial',
    cupo_maximo         INT NULL,
    lista_espera_maxima INT NULL,
    activo              TINYINT(1) NOT NULL DEFAULT 1,
    UNIQUE KEY uq_comision (id_periodo, id_materia, codigo),
    CONSTRAINT fk_comision_materia FOREIGN KEY (id_materia) REFERENCES materias(id_materia),
    CONSTRAINT fk_comision_periodo FOREIGN KEY (id_periodo) REFERENCES periodos(id_periodo)
);

CREATE TABLE inscripciones_comision (
    id_inscripcion_comision INT AUTO_INCREMENT PRIMARY KEY,
    id_inscripcion_carrera  INT NOT NULL,
    id_comision             INT NOT NULL,
    estado                  ENUM('en_espera','inscripto','baja') NOT NULL DEFAULT 'inscripto',
    condicion               ENUM('regular','libre') NULL,
    fecha_inscripcion       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_inscripcion_comision (id_inscripcion_carrera, id_comision),
    CONSTRAINT fk_icomision_insc_carrera FOREIGN KEY (id_inscripcion_carrera) REFERENCES inscripciones_carrera(id_inscripcion_carrera),
    CONSTRAINT fk_icomision_comision     FOREIGN KEY (id_comision) REFERENCES comisiones(id_comision)
);

CREATE TABLE examenes (
    id_examen   INT AUTO_INCREMENT PRIMARY KEY,
    id_materia  INT NOT NULL,
    id_periodo  INT NULL,
    fecha       DATETIME NOT NULL,
    aula        VARCHAR(60) NULL,
    CONSTRAINT fk_examen_materia FOREIGN KEY (id_materia) REFERENCES materias(id_materia),
    CONSTRAINT fk_examen_periodo FOREIGN KEY (id_periodo) REFERENCES periodos(id_periodo)
);

CREATE TABLE inscripciones_examen (
    id_inscripcion_examen  INT AUTO_INCREMENT PRIMARY KEY,
    id_inscripcion_carrera INT NOT NULL,
    id_examen              INT NOT NULL,
    estado                 ENUM('inscripto','presente','ausente') NOT NULL DEFAULT 'inscripto',
    nota                   DECIMAL(4,2) NULL,
    fecha_inscripcion      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_inscripcion_examen (id_inscripcion_carrera, id_examen),
    CONSTRAINT fk_iexamen_insc_carrera FOREIGN KEY (id_inscripcion_carrera) REFERENCES inscripciones_carrera(id_inscripcion_carrera),
    CONSTRAINT fk_iexamen_examen       FOREIGN KEY (id_examen) REFERENCES examenes(id_examen)
);

CREATE TABLE certificados (
    id_certificado         INT AUTO_INCREMENT PRIMARY KEY,
    id_inscripcion_carrera INT NOT NULL,
    tipo                   ENUM('analitico','constancia_inscripcion','alumno_regular') NOT NULL,
    fecha_generacion       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ruta_archivo           VARCHAR(255) NULL,
    -- Código público (impreso en el QR) que permite validar la autenticidad del documento.
    codigo_validacion      CHAR(32) NOT NULL,
    UNIQUE KEY uq_certificado_codigo (codigo_validacion),
    CONSTRAINT fk_certificado_insc_carrera FOREIGN KEY (id_inscripcion_carrera) REFERENCES inscripciones_carrera(id_inscripcion_carrera)
);

-- FK de asignaciones_docentes -> comisiones agregada por ALTER,
-- porque `comisiones` se define después de `asignaciones_docentes`.
-- Semántica: id_comision = NULL -> asignación a nivel materia (todas sus comisiones).
ALTER TABLE asignaciones_docentes
    ADD CONSTRAINT fk_asigdoc_comision FOREIGN KEY (id_comision) REFERENCES comisiones(id_comision);
