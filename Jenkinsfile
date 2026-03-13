pipeline {
    agent any
    options {
        skipDefaultCheckout(false)
    }
    environment {
        JELLYFIN_CONTAINER = 'jellyfin'
        PROJECT_NAME = 'JFolderCollection'
        INTERNAL_PLUGIN_PATH = "/config/plugins/${PROJECT_NAME}"
    }
    stages {
        stage('01- Checkout') {
            steps {
                checkout([$class: 'GitSCM', 
                    branches: scm.branches, 
                    extensions: [[$class: 'WipeWorkspace']], 
                    userRemoteConfigs: scm.userRemoteConfigs
                ])
            }
        }
        stage('01.1 - Versioning') {
            steps {
                script {
                    def fullVersion = "1.0.0.${env.BUILD_NUMBER}"
                    echo "📌 Definindo versão: ${fullVersion}"
                    sh "sed -i 's/BUILD_VERSION/${fullVersion}/g' manifest.json"
                    sh "sed -i 's/BUILD_VERSION/${fullVersion}/g' JFolderCollection/Configuration/configPage.html"
                }
            }
        }
        stage('02- Build Plugin') {
            steps {
                script {
                    echo "🛠️ Compilando via Dockerfile..."
                    sh "docker build --no-cache --pull --build-arg VERSION=1.0.0.${env.BUILD_NUMBER} -t jfolder-builder:latest ."
                    sh "docker create --name temp-jfolder jfolder-builder:latest"
                    sh "mkdir -p ./publish"
                    sh "docker cp temp-jfolder:/app/. ./publish"
                    sh "docker rm temp-jfolder"
                }
            }
        }

         stage('03- Deploy & Restart') {
            steps {
                script {
                    // 1. Garantir que o container esteja rodando para aceitar o comando 'exec'
                    // Se ele já estiver rodando, não faz nada. Se estiver parado, ele liga.
                    sh "docker start jellyfin || true"

                    echo "🔥 OPERAÇÃO TERRA ARRASADA: Limpando a pasta do plugin..."
                    // Usamos o container LIGADO para limpar o conteúdo
                    sh "docker exec -u 0 jellyfin rm -rf /config/plugins/JFolderCollection/*"

                    echo "🚀 Injetando v1.0.0.${env.BUILD_NUMBER}..."
                    sh "docker cp ./publish/. jellyfin:/config/plugins/JFolderCollection/"
            
                    echo "🔑 Ajustando permissões..."
                    sh "docker exec -u 0 jellyfin chown -R 1000:1000 /config/plugins/JFolderCollection"

                    echo "🔄 Reiniciando Jellyfin para aplicar as mudanças..."
                    // O restart para e liga o processo, garantindo que a nova DLL seja carregada
                    sh "docker restart jellyfin"
                }
            }
        }
    }
    post {
        always {
            sh "rm -rf publish || true"
            cleanWs()
        }
    }
}