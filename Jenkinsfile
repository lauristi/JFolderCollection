pipeline {
    agent any

    environment {
        PROJECT_NAME = 'JFolderCollection'
        JELLYFIN_CONTAINER = 'jellyfin'
        INTERNAL_PLUGIN_PATH = "/config/plugins/${PROJECT_NAME}"
        // Imagem oficial da Microsoft que já tem tudo para compilar C#
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
                    echo "🛠️ Compilando o plugin usando container temporário do .NET..."
                    
                    // Este comando roda um container do .NET, monta o código dentro dele, 
                    // compila e salva o resultado na pasta ./publish do seu host.
                    sh """
                        docker run --rm \
                        -v \$(pwd):/src \
                        -w /src \
                        ${env.DOTNET_SDK_IMAGE} \
                        dotnet publish ${env.PROJECT_NAME}/${env.PROJECT_NAME}.csproj -c Release -o ./publish
                    """
                }
            }
        }

        stage('03- Deploy') {
            steps {
                script {
                    echo "📦 Injetando DLL no Jellyfin..."
                    
                    // 1. Garante a pasta no Jellyfin
                    sh "docker exec -u 0 ${env.JELLYFIN_CONTAINER} mkdir -p ${env.INTERNAL_PLUGIN_PATH}"
                    
                    // 2. Copia a DLL gerada no estágio anterior
                    sh "docker cp ./publish/. ${env.JELLYFIN_CONTAINER}:${env.INTERNAL_PLUGIN_PATH}/"
                    
                    // 3. Ajusta o dono (seu usuário 1000)
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