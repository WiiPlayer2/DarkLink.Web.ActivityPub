pipeline {
    agent {
        docker { image 'mcr.microsoft.com/dotnet/sdk:6.0' }
    }

    environment {
        DOTNET_CLI_HOME = '/tmp/DOTNET_CLI_HOME'
    }

    stages {
        stage('Cleanup') {
            steps {
                sh 'rm -r ./packages || true'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build ./DarkLink.Web.ActivityPub.sln'
            }
        }

        stage('Pack') {
            steps {
                sh 'dotnet pack --no-build ./DarkLink.Web.ActivityPub.sln --output ./packages --version-suffix $(date +%s)'
            }
        }

        stage('Publish') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'private-nuget-repo', passwordVariable: 'apiKey', usernameVariable: 'source')]) {
                    sh "dotnet nuget push ./packages/* --skip-duplicate --source $source --api-key $apiKey"
                }
            }
        }
    }
}
