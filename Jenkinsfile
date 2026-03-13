pipeline {
    agent any
    options {
        // Isso ajuda a evitar o cache de versões antigas do script
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
                // Voltamos ao comando padrão, mas com o WipeWorkspace para limpar o cache
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

                    // Agora na raiz, o Jenkins encontra de primeira
                    sh "sed -i 's/BUILD_VERSION/${fullVersion}/g' manifest.json"
            
                    // O HTML continua na subpasta onde o código mora
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
                    echo "🚀 Injetando no Jellyfin..."
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} mkdir -p ${env.INTERNAL_PLUGIN_PATH}"
                    sh "docker cp ./publish/. ${env.JELLYFIN_CONTAINER}:${env.INTERNAL_PLUGIN_PATH}/"
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} chown -R 1000:1000 ${env.INTERNAL_PLUGIN_PATH}"
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