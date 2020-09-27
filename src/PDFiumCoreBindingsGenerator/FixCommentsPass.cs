using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using CppSharp.AST;
using CppSharp.Passes;

namespace PDFiumCoreBindingsGenerator
{
    /// <summary>
    /// Pass that makes Xml Documentation Comments almost Xml Documentation Comments again.
    /// </summary>
    /// <remarks>
    /// But has a bunch of para cruft that doesn't seem to be removable.
    /// </remarks>
    public class FixCommentsPass : TranslationUnitPass
    {
        private readonly Regex m_rx = new Regex(@"///(?<text>.*)",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private Dictionary<string, string[]> _headerFiles = new Dictionary<string, string[]>();

        private enum CommentSection
        {
            Unset,
            Function,
            Parameters,
            ReturnValue,
            Comments
        }

        public override bool VisitDeclaration(CppSharp.AST.Declaration declaration)
        {
            if (AlreadyVisited(declaration))
                return false;



            if (declaration is Function && !(declaration is Method))
            {
                var s = (TranslationUnit) declaration.OriginalNamespace;
                if (!_headerFiles.ContainsKey(s.FilePath))
                    _headerFiles.Add(s.FilePath, File.ReadAllLines(s.FilePath));


                var commentLines = new Stack<string>();

                for (int i = declaration.LineNumberStart - 2; i >= 0; i--)
                {
                    commentLines.Push(_headerFiles[s.FilePath][i]);

                    if (_headerFiles[s.FilePath][i].StartsWith("// Function: "))
                        break;

                    if (string.IsNullOrWhiteSpace(_headerFiles[s.FilePath][i]))
                        break;
                }

                var comments = commentLines.ToArray();

                var content = new List<InlineContentComment>();
                TextComment lastComment = null;
                CommentSection commentSection = CommentSection.Unset;
                foreach (var comment in comments)
                {
                    var writeComment = comment;
                    if(string.IsNullOrEmpty(comment))
                        continue;

                    if (writeComment.StartsWith("// ")
                     || writeComment.StartsWith(" * ")
                     || writeComment.StartsWith(" */"))
                    {
                        writeComment = comment.Substring(3);
                    }
                    else if (writeComment.StartsWith("//")
                        || writeComment.StartsWith(" *")
                        || writeComment.StartsWith("* ")
                        || writeComment.StartsWith("/*")
                        || writeComment.StartsWith("*/"))
                    {
                        writeComment = comment.Substring(2);
                    }


                    // See if this is a new section
                    if (writeComment.Length > 0 && !char.IsWhiteSpace(writeComment[0]))
                    {
                        if (writeComment.Trim() == "Function:")
                        {
                            commentSection = CommentSection.Function;
                        }
                        else if (writeComment.Trim() == "Parameters:")
                        {
                            commentSection = CommentSection.Parameters;
                        }
                        else if (writeComment.Trim() == "Return value:")
                        {
                            commentSection = CommentSection.ReturnValue;
                        }
                        else if (writeComment.Trim() == "Comments:")
                        {
                            commentSection = CommentSection.Comments;
                        }
                    }

                    if (commentSection == CommentSection.Parameters)
                    {
                        if (writeComment.StartsWith("            ") && lastComment != null)
                        {
                            lastComment.Text += " " + writeComment.TrimStart();
                            continue;
                        }
                    }
                    else if (commentSection == CommentSection.Comments)
                    {
                        if (writeComment.StartsWith("    ") && lastComment != null)
                        {
                            lastComment.Text += " " + writeComment.TrimStart();
                            continue;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(writeComment))
                            continue;
                    }
                    
                    lastComment = new TextComment()
                    {
                        Text = writeComment,
                        HasTrailingNewline = true
                    };

                    content.Add(lastComment);
                }
                declaration.Comment = new RawComment()
                {
                    FullComment = new FullComment
                    {
                        Blocks = new List<BlockContentComment>()
                        {
                            new ParagraphComment
                            {
                                Content = content
                            }
                        },

                    }
                };
            }

            return true;


            /*
            if (declaration.Comment != null)
            {
                var fullComment = declaration.Comment.FullComment;
                fullComment.Blocks.Clear();
    
                var summaryPara = new ParagraphComment();
    
                if (declaration.LogicalOriginalName.Contains("FPDFBitmap"))
                {
    
                }
    
                summaryPara.Content.Add(new TextComment()
                {
                    
                    Text = ""
                });
                fullComment.Blocks.Add(summaryPara);
    
                /*
                var remarksElement = xRoot.Element("remarks");
                if (remarksElement != null)
                {
                    foreach (var remarksLine in remarksElement.Value.Split('\n'))
                    {
                        var remarksPara = new ParagraphComment();
                        remarksPara.Content.Add(new TextComment
                        {
                            Text = remarksLine.ReplaceLineBreaks("").Trim()
                        });
                        fullComment.Blocks.Add(remarksPara);
                    }
                }
    
                var paramElements = xRoot.Elements("param");
                foreach (var paramElement in paramElements)
                {
                    var paramComment = new ParamCommandComment();
                    paramComment.Arguments.Add(new BlockCommandComment.Argument
                    {
                        Text = paramElement.Attribute("name").Value
                    });
                    paramComment.ParagraphComment = new ParagraphComment();
                    StringBuilder paramTextCommentBuilder = new StringBuilder();
                    foreach (var paramLine in paramElement.Value.Split('\n'))
                    {
                        paramTextCommentBuilder.Append(paramLine.ReplaceLineBreaks("").Trim() + " ");
                    }
                    paramComment.ParagraphComment.Content.Add(new TextComment
                    {
                        Text = paramTextCommentBuilder.ToString()
                    });
                    fullComment.Blocks.Add(paramComment);
                }
            }
            /*
            //Fix Enum comments
            if (declaration is Enumeration enumDecl)
            {
                foreach (var item in enumDecl.Items.Where(i => i.Comment != null))
                {
                    item.Comment.BriefText = item.Comment.BriefText.Replace("<summary>", "").Replace("</summary>", "").Trim();
                }
            }*/
        }

        private XDocument GetOriginalDocumentationDocument(string documentationText)
        {
            var descriptionXmlBuilder = new StringBuilder();
            descriptionXmlBuilder.AppendLine("<description>");
            foreach (Match match in m_rx.Matches(documentationText))
            {
                var text = match.Groups["text"].Value;
                descriptionXmlBuilder.Append(text);
            }

            descriptionXmlBuilder.AppendLine("</description>");
            var descriptionXDoc = XDocument.Parse(descriptionXmlBuilder.ToString());
            return descriptionXDoc;
        }
    }
}