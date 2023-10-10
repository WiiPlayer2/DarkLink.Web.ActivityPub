def runAll()
{
    def configuredBuild = load "ci/jenkins/configuredBuild.groovy";
    configuredBuild.runAll();
}

pipeline
{
    agent {
        docker { image 'mcr.microsoft.com/dotnet/sdk:6.0' }
    }

    stages { stage('All') { steps { sh 'printenv | sort'; script { runAll(); } } } }
}
