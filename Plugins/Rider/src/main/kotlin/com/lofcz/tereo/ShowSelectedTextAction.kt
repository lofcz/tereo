package com.lofcz.tereo

import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.editor.Editor
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.intellij.psi.util.elementType
import com.intellij.psi.PsiWhiteSpace
import com.intellij.psi.xml.XmlText
import com.intellij.psi.PsiDocumentManager
import com.intellij.psi.html.HtmlTag
import com.intellij.psi.xml.XmlTag

class ShowSelectedTextAction : AnAction() {

    override fun getActionUpdateThread(): ActionUpdateThread {
        return ActionUpdateThread.EDT
    }

    override fun update(e: AnActionEvent) {
        val editor = e.getData(CommonDataKeys.EDITOR)
        val project = e.getData(CommonDataKeys.PROJECT)

        if (editor == null || project == null) {
            e.presentation.isVisible = false
            return
        }

        val psiFile = PsiDocumentManager.getInstance(project)
            .getPsiFile(editor.document)

        if (psiFile == null) {
            e.presentation.isVisible = false
            return
        }

        val selectionModel = editor.selectionModel
        if (!selectionModel.hasSelection()) {
            e.presentation.isVisible = false
            return
        }

        val element = findElementAtSelection(editor, psiFile)
        val isValid = isValidElement(element);

        e.presentation.isVisible = isValid;
    }

    override fun actionPerformed(e: AnActionEvent) {
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val project = e.getData(CommonDataKeys.PROJECT) ?: return

        val selectionModel = editor.selectionModel
        if (!selectionModel.hasSelection()) return

        val start = selectionModel.selectionStart
        val end = selectionModel.selectionEnd
        val document = editor.document

        val psiFile = PsiDocumentManager.getInstance(project).getPsiFile(editor.document)
        val element = psiFile?.findElementAt(start)

        val replacement = getReplacement(element)

        com.intellij.openapi.command.WriteCommandAction.runWriteCommandAction(
            project,
            "Replace with Reo.TestReplacement",
            null,
            Runnable {
                document.replaceString(start, end, replacement)
                editor.caretModel.moveToOffset(start + replacement.length)
                selectionModel.removeSelection()
            }
        )
    }

    private fun getReplacement(element: PsiElement?): String {
        if (element == null) return "Reo.TestReplacement"

        val file = element.containingFile
        val fileName = file?.name ?: ""

        // Kontrola, zda jsme v Razor souboru
        if (!fileName.endsWith(".razor") && !fileName.endsWith(".cshtml")) {
            return "Reo.TestReplacement"
        }

        // Zjištění kontextu pomocí fileType
        val fileType = file.fileType.name
        return when (fileType) {
            "HTML" -> "@Reo.TestReplacement"  // Jsme v markup kontextu
            "C#" -> "Reo.TestReplacement"     // Jsme v C# kontextu
            else -> "Reo.TestReplacement"
        }
    }

    private fun findElementAtSelection(editor: Editor, file: PsiFile): PsiElement? {
        val selectionStart = editor.selectionModel.selectionStart
        return file.findElementAt(selectionStart)
    }

    private fun isValidElement(element: PsiElement?): Boolean {
        if (element == null) return false

        val file = element.containingFile
        val fileName = file?.name ?: ""
        val fileType = file?.fileType?.name ?: ""

        fun checkInterpolatedString(element: PsiElement): Boolean? {
            var localCurrent: PsiElement? = element
            while (localCurrent != null) {
                when (localCurrent.elementType.toString()) {
                    "INTERPOLATED_STRING_LITERAL_EXPRESSION" -> {
                        // Kontrolujeme, zda některé z dětí obsahuje interpolaci
                        val hasInterpolatedSections = localCurrent.children.any { child ->
                            child.elementType.toString() == "INTERPOLATED_STRING_LITERAL_EXPRESSION_PART" &&
                                    child.children.any {
                                        val elementType = it.elementType.toString()
                                        elementType != "INTERPOLATED_STRING_TEXT" &&
                                                elementType != "INTERPOLATED_STRING_REGULAR" &&
                                                elementType != "RAZOR_FRAGMENT"
                                    }
                        }
                        return hasInterpolatedSections
                    }
                    "INTERPOLATED_STRING_LITERAL_EXPRESSION_PART" -> {
                        // Pokud jsme přímo v části a obsahuje něco jiného než text, je to interpolace
                        return localCurrent.children.any {
                            val elementType = it.elementType.toString()
                            elementType != "INTERPOLATED_STRING_REGULAR" &&
                                    elementType != "RAZOR_FRAGMENT"
                        }
                    }
                }
                localCurrent = localCurrent.parent
            }
            return null
        }


        // Jsme v Razor souboru
        if (fileName.endsWith(".razor") || fileName.endsWith(".cshtml")) {
            when (fileType) {
                "HTML",
                "C#" -> {
                    // Kontrola pro C# kontext
                    var current: PsiElement? = element
                    while (current != null) {
                        val elementType = current.elementType.toString()

                        when (checkInterpolatedString(current)) {
                            true -> return false  // Je to interpolovaný string s interpolacemi
                            false -> return true  // Je to interpolovaný string bez interpolací
                            null -> {
                                // Pokračujeme v kontrole ostatních typů stringů
                                if (elementType.contains("STRING_LITERAL") ||
                                    elementType.contains("STRING")) {
                                    return true
                                }
                            }
                        }
                        current = current.parent
                    }
                }
            }
        }

        // Původní logika pro ostatní typy souborů
        return isValidElementOriginal(element)
    }

    private fun isValidElementOriginal(element: PsiElement?): Boolean {
        if (element == null) return false

        var current: PsiElement? = element
        while (current != null) {
            // Získáme typ elementu a jeho text pro debugging
            val elementType = current.elementType.toString()
            println("Checking element: $elementType, Text: ${current.text}")

            when {
                // Ignorujeme whitespace
                current is PsiWhiteSpace -> {
                    current = current.parent
                    continue
                }

                // HTML/XML elementy
                current is XmlText -> return true
                current is HtmlTag -> return true
                current is XmlTag -> return true

                elementType == "INTERPOLATED_STRING_LITERAL_EXPRESSION" -> {
                    // Kontrola, zda string obsahuje interpolované sekce
                    val hasInterpolatedSections = current.children.any {
                        it.elementType.toString() == "INTERPOLATED_STRING_EXPRESSION"
                    }

                    // Pokud nemá interpolované sekce, chováme se jako k běžnému stringu
                    // Pokud má interpolované sekce, zamítneme překlad
                    return !hasInterpolatedSections
                }

                // Kontrola typu elementu podle stringu
                elementType in setOf(
                    // HTML/Razor elementy
                    "XML_TEXT",
                    "HTML_TAG",
                    "RAZOR_TEXT",
                    "XML_TAG",
                    "XML_DATA_CHARACTERS",

                    // Stringy v různých jazycích
                    "STRING_LITERAL_REGULAR",
                    "STRING_LITERAL",
                    "STRING_LITERAL_EXPRESSION",
                    "SINGLE_QUOTED_STRING",
                    "DOUBLE_QUOTED_STRING",
                    "JS:STRING_LITERAL",
                    "TS:STRING_LITERAL",
                    "CSS_STRING",
                    "TEXT",

                    // Razor specifické
                    "RAZOR_TEXT_BLOCK",
                    "RAZOR_TEMPLATE_TEXT"
                ) -> return true

                // Kontrola rodičovského elementu pro HTML/XML
                current.parent?.elementType.toString() in setOf(
                    "HTML_TAG",
                    "XML_TAG",
                    "RAZOR_BLOCK"
                ) -> return true
            }

            // Kontrola, zda jsme v kódu, který chceme vyloučit
            if (isCodeElement(current)) {
                return false
            }

            current = current.parent
        }

        return false
    }

    private fun isCodeElement(element: PsiElement): Boolean {
        val elementType = element.elementType.toString()

        return elementType in setOf(
            // C#
            "METHOD_DECLARATION",
            "CLASS_DECLARATION",
            "NAMESPACE_DECLARATION",

            // JavaScript/TypeScript
            "JS:FUNCTION_DECLARATION",
            "JS:CLASS_DECLARATION",
            "TS:FUNCTION_DECLARATION",
            "TS:CLASS_DECLARATION",

            // Generic
            "METHOD_CALL_EXPRESSION",
            "REFERENCE_EXPRESSION",
            "FUNCTION_EXPRESSION",
            "LAMBDA_EXPRESSION"
        )
    }
}