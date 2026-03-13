pipeline {
    agent any

    environment {
        PROJECT_NAME = 'JFolderCollection'
        JELLYFIN_CONTAINER = 'jellyfin'
        INTERNAL_PLUGIN_PATH = "/config/plugins/${PROJECT_NAME}"
        DOTNET_SDK_IMAGE = 'mcr.microsoft.com/dotnet/sdk:9.0'
    }

    stages {
        stage('01- Checkout') {
            steps {
                checkout scm
            }
        }

        stage('02- Build (via Docker)') {
            steps {
                script {
                    echo "🛠️ Compilando a solução..."
                    sh "docker run --rm -v \$(pwd):/src -w /src ${env.DOTNET_SDK_IMAGE} dotnet publish JFolderCollection.sln -c Release -o ./publish"
                }
            }
        }

        stage('03- Deploy') {
            steps {
                script {
                    echo "📦 Injetando DLL no Jellyfin..."
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} mkdir -p ${env.INTERNAL_PLUGIN_PATH}"
                    sh "docker cp ./publish/. ${env.JELLYFIN_CONTAINER}:${env.INTERNAL_PLUGIN_PATH}/"
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} chown -R 1000:1000 ${env.INTERNAL_PLUGIN_PATH}"
                }
            }
        }

        stage('04- Restart') {
            steps {
                sh "docker restart ${env.JELLYFIN_CONTAINER}"
            }
        }
    }

    post {
        always {
            cleanWs()
        }
    }
}