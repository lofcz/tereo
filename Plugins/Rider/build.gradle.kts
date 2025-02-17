import org.jetbrains.changelog.Changelog
import org.jetbrains.changelog.markdownToHTML
import org.jetbrains.intellij.platform.gradle.IntelliJPlatformType
import org.jetbrains.intellij.platform.gradle.extensions.intellijPlatform

tasks.wrapper {
    gradleVersion = "8.12.1"
    // You can either download the binary-only version of Gradle (BIN) or
    // the full version (with sources and documentation) of Gradle (ALL)
    distributionType = Wrapper.DistributionType.ALL
}

fun properties(key: String) = providers.gradleProperty(key)
fun environment(key: String) = providers.environmentVariable(key)

plugins {
    id("java") // Java support
    id("me.filippov.gradle.jvm.wrapper") version "0.14.0"
    id("org.jetbrains.changelog") version "2.2.1"
    id("org.jetbrains.intellij.platform") version "2.2.1"
    id("org.jetbrains.kotlin.jvm") version "2.0.20"
}

group = properties("pluginGroup").get()
version = properties("pluginVersion").get()
val sinceBuildVersion = properties("pluginSinceBuild")

// Configure project's dependencies
repositories {
    mavenCentral()

    intellijPlatform {
        defaultRepositories()
    }
}

intellijPlatform {
    pluginVerification {
        ides {
            ide(IntelliJPlatformType.Rider, "2024.3.5")
        }
    }
}

dependencies {

    intellijPlatform {
        rider("2024.3.5")
    }

    implementation("de.undercouch:bson4jackson:2.15.1")
    implementation("com.github.pgreze:kotlin-process:1.5")
    implementation("com.fasterxml.jackson.module:jackson-module-kotlin:2.18.+")
    testImplementation("org.testng:testng:7.10.2")
}


val riderSdkVersion: String by project

// Configure Gradle Changelog Plugin – read more: https://github.com/JetBrains/gradle-changelog-plugin
changelog {
    groups.empty()
    repositoryUrl = properties("pluginRepositoryUrl")
}

tasks {
    wrapper {
        gradleVersion = properties("gradleVersion").get()
    }

    signPlugin {
        certificateChain = environment("CERTIFICATE_CHAIN")
        privateKey = environment("PRIVATE_KEY")
        password = environment("PRIVATE_KEY_PASSWORD")
    }

    publishPlugin {
        dependsOn("patchChangelog")
        token = environment("PUBLISH_TOKEN")
        // The pluginVersion is based on the SemVer (https://semver.org) and supports pre-release labels, like 2.1.7-alpha.3
        // Specify pre-release label to publish the plugin in a custom Release Channel automatically. Read more:
        // https://plugins.jetbrains.com/docs/intellij/deployment.html#specifying-a-release-channel
        channels = properties("pluginVersion").map { listOf(it.split('-').getOrElse(1) { "default" }.split('.').first()) }
    }

    patchPluginXml {
        // version.set("${project.version}")
        sinceBuild.set(sinceBuildVersion)
        untilBuild.set(provider { null })
    }
}
