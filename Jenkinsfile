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
                    sh "docker build --no-cache --build-arg VERSION=1.0.0.${env.BUILD_NUMBER} -t jfolder-builder:latest ."
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
                    echo "🧹 Limpando a pasta do plugin no volume mapeado..."
                    // Limpa direto no container para garantir que o volume no SSD seja afetado
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} sh -c 'rm -rf ${env.INTERNAL_PLUGIN_PATH}/*'"

                    echo "🚀 Injetando v1.0.0.${env.BUILD_NUMBER}..."
                    sh "docker cp ./publish/. ${env.JELLYFIN_CONTAINER}:${env.INTERNAL_PLUGIN_PATH}/"
            
                    echo "🔑 Ajustando permissões..."
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} chown -R 1000:1000 ${env.INTERNAL_PLUGIN_PATH}"

                    echo "🔄 Forçando o Jellyfin a recarregar..."
                    // Em vez de apenas 'restart', vamos dar um 'stop' e 'start' para garantir o ciclo completo
                    sh "docker stop ${env.JELLYFIN_CONTAINER}"
                    sh "docker start ${env.JELLYFIN_CONTAINER}"
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