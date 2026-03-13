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
                    echo "🧹 FAXINA TOTAL: Removendo e recriando a pasta do plugin..."
                    
                    // Em vez de apagar o conteúdo (*), removemos a pasta INTEIRA e a recriamos.
                    // Isso força o sistema de arquivos do SSD a limpar os inodes antigos.
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} rm -rf ${env.INTERNAL_PLUGIN_PATH}"
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} mkdir -p ${env.INTERNAL_PLUGIN_PATH}"
                    
                    echo "🚀 Injetando v1.0.0.${env.BUILD_NUMBER} no Jellyfin..."
                    sh "docker cp ./publish/. ${env.JELLYFIN_CONTAINER}:${env.INTERNAL_PLUGIN_PATH}/"
                    
                    // Ajuste de permissão essencial para o Jellyfin conseguir ler
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} chown -R 1000:1000 ${env.INTERNAL_PLUGIN_PATH}"
                    
                    echo "🔄 Reiniciando o container..."
                    sh "docker restart ${env.JELLYFIN_CONTAINER}"
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