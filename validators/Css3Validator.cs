using System;
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;       
using ExCSS;

namespace AutomatedAssignmentValidator{
    class Css3Validator: ValidatorBaseHtml5{      
        public Css3Validator(string studentFolder): base(studentFolder){                        
        }  
        public override List<TestResult> Validate()
        {            
            ClearResults();
            if(LoadHtml5Document("index.html")){     
                Terminal.Indent();

                OpenTest("Validating inline CSS... ");
                CloseTest(CheckInlineCss());    

                OpenTest("Validating the divs... ");
                CloseTest(CheckDivs());
                        
                OpenTest("Validating the video... ");
                CloseTest(CheckVideo());                                                
            
                Terminal.BreakLine();
                Terminal.UnIndent();

                if(LoadCss3Document("index.css")){     
                    Terminal.Indent();              
                    CloseTest(CheckCssProperty("font"));
                    CloseTest(CheckCssProperty("border"));
                    CloseTest(CheckCssProperty("text"));
                    CloseTest(CheckCssProperty("color"));
                    CloseTest(CheckCssProperty("background"));
                    CloseTest(CheckCssProperty("float", "left"));
                    CloseTest(CheckCssProperty("float", "right"));
                    CloseTest(CheckCssProperty("position", "absolute"));
                    CloseTest(CheckCssProperty("position", "relative"));
                    CloseTest(CheckCssProperty("clear"));
                    CloseTest(CheckCssProperty("width"));
                    CloseTest(CheckCssProperty("height"));
                    CloseTest(CheckCssProperty("margin"));
                    CloseTest(CheckCssProperty("padding"));
                    CloseTest(CheckCssProperty("list"));

                    //Just one needed
                    OpenTest("Validating 'top / right / left / bottom' style... ");
                    List<string> top = CheckCssProperty("top", null, false);
                    List<string> right = CheckCssProperty("right", null, false);
                    List<string> left = CheckCssProperty("left", null, false);
                    List<string> bottom = CheckCssProperty("bottom", null, false);
                    if(top.Count == 0 || right.Count == 0 || left.Count == 0 || bottom.Count == 0) CloseTest(new List<string>());
                    else CloseTest(top.Concat(right).Concat(left).Concat(bottom).ToList());                    
                }
            }
            
            PrintScore();                    
            Terminal.UnIndent();
            
            return GlobalResults;
        }        
        private List<string> CheckDivs(){
            List<string> errors = new List<string>();

            try{
                HtmlNodeCollection nodes = this.HtmlDoc.DocumentNode.SelectNodes("//div");
                if(nodes == null || nodes.Count < 1) errors.Add("Does not contains any div.");                            
            }
            catch(Exception e){
                errors.Add(string.Format("EXCEPTION: {0}", e.Message));
            }
        
            return errors;
        }
        private List<string> CheckVideo(){
            List<string> errors = new List<string>();

            try{
                bool video = CheckVideo("video");
                bool iframe = CheckVideo("iframe", "src", "youtube.com");
                bool @object = CheckVideo("object", "data", "youtube.com");

                if(!video && !iframe && !@object) errors.Add(string.Format("Unable to find any video pointing to some of the following streaming services: {0}.", "youtube.com"));
            }
            catch(Exception e){
                errors.Add(string.Format("EXCEPTION: {0}", e.Message));
            }                       

            return errors;
        } 
        private bool CheckVideo(string node, string attribute = null, string url = null){
            //TODO: url must be a list of valid values
            HtmlNodeCollection nodes = this.HtmlDoc.DocumentNode.SelectNodes(string.Format("//{0}", node));
            if(nodes != null && nodes.Count > 0){
                if(string.IsNullOrEmpty(attribute)) return true;
                foreach(HtmlNode n in nodes){
                    string value = n.GetAttributeValue(attribute, "");
                    if(!string.IsNullOrEmpty(value)){
                        if(string.IsNullOrEmpty(url)) return true;
                        else if(value.Contains(url)) return true;
                    }                    
                }
            }  

            return false;
        }

        private List<string> CheckInlineCss(){
            List<string> errors = new List<string>();

            try{
                HtmlNodeCollection nodes = this.HtmlDoc.DocumentNode.SelectNodes("//style");           
                if(nodes != null && nodes.Count > 0) errors.Add("CSS definition found using the Style tag.");
                
                foreach(HtmlNode node in this.HtmlDoc.DocumentNode.DescendantsAndSelf()){
                    if(!string.IsNullOrEmpty(node.GetAttributeValue("style", ""))){
                        errors.Add("CSS definition found using inline declarations with style attributes.");
                        break;
                    }
                }                
            }
            catch(Exception e){
                errors.Add(string.Format("EXCEPTION: {0}", e.Message));
            }
            
            return errors;
        }

        private List<string> CheckCssProperty(string property, string value = null, bool verbose = true){  
            List<string> errors = new List<string>();

            try{
                if(verbose && string.IsNullOrEmpty(value)) OpenTest(string.Format("Validating '{0}' style... ", property));
                else if(verbose) OpenTest(string.Format("Validating '{0}:{1}' style... ", property, value));

                bool found = false;
                bool applied = false;
                foreach(StylesheetNode cssNode in this.CssDoc.Children){
                    if(!CssNodeUsingProperty(cssNode, property, value)) continue;
                    found = true;

                    //Checking if the given css style is being used. Important: only one selector is allowed when calling BuildXpathQuery, so comma split is needed
                    string[] selectors = GetCssSelectors(cssNode);
                    foreach(string s in selectors){
                        HtmlNodeCollection htmlNodes = this.HtmlDoc.DocumentNode.SelectNodes(BuildXpathQuery(s));
                        if(htmlNodes != null && htmlNodes.Count > 0){
                            applied = true;
                            break;
                        }                     
                    }     

                    if(applied) break; 
                }
                    
                if(!found) errors.Add("Unable to find the style within the CSS file.");
                else if(!applied) errors.Add("The style has been found applied the CSS but it's not being applied into the HTML document.");
               
            }
            catch(Exception e){
                errors.Add(string.Format("EXCEPTION: {0}", e.Message));
            }            

            return errors;
        }       
        private string[] GetCssSelectors(StylesheetNode node){
            string css = node.ToCss();
            return css.Substring(0, css.IndexOf("{")).Trim().Split(',');
        }        
        private List<string[]> GetCssContent(StylesheetNode node){
            List<string[]> lines = new List<string[]>();
            string css = node.ToCss();

            css = css.Substring(css.IndexOf("{")+1);            
            foreach(string line in css.Split(";")){
                if(line.Contains(":")){
                    string[] item = line.Replace(" ", "").Split(":");
                    if(item[1].Contains("}")) item[1]=item[1].Replace("}", "");
                    if(item[1].Length > 0) lines.Add(item);
                }                
            }

            return lines;
        }
        private bool CssNodeUsingProperty(StylesheetNode node, string property, string value = null){
            List<string[]> definition = GetCssContent(node);
            foreach(string[] line in definition){
                //If looking for "margin", valid values are: margin and margin-x
                //If looking for "top", valid values are just top
                //So, the property must be alone or left-sided over the "-" symbol.
                if(line[0].Contains(property) && (!line[0].Contains("-") || line[0].Split("-")[0] == property)){                                        
                    if(value == null) return true;
                    else if(line[1].Contains(value)) return true;
                }
            }

            return false;
        }
        private string BuildXpathQuery(string cssSelector){
            //TODO: if a comma is found, build the correct query with ORs (check first if it's supported by HtmlAgilitypack)
            string xPathQuery = ".";
            string[] selectors = cssSelector.Trim().Replace(">", " > ").Split(' '); //important to force spaces between ">"

            bool children = false;
            for(int i = 0; i < selectors.Length; i++){
                //ignoring modifiers like ":hover"
                if(selectors[i].Contains(":")) selectors[i] = selectors[i].Substring(0, selectors[i].IndexOf(":"));
                
                if(selectors[i].Substring(1).Contains(".")){
                    //Recursive case: combined selectors like "p.bold" (wont work with multi-class selectors)
                    int idx = selectors[i].Substring(1).IndexOf(".")+1;
                    string left = BuildXpathQuery(selectors[i].Substring(0, idx));
                    string right = BuildXpathQuery(selectors[i].Substring(idx));

                    left = left.Substring(children ? 2 : 3);
                    if(left.StartsWith("*")) xPathQuery = right + left.Substring(1);
                    else xPathQuery = right.Replace("*", left);                        
                }
                else{
                    //Base case
                    if(selectors[i].StartsWith("#") || selectors[i].StartsWith(".")){                    
                        xPathQuery += string.Format("{0}*[@{1}='{2}']", (children ? "/" : "//"), (selectors[i].StartsWith("#") ? "id" : "class"), selectors[i].Substring(1));
                        children = false; 
                    }                
                    else if(selectors[i].StartsWith(">")){
                        children = true;
                    }
                    else if(!string.IsNullOrEmpty(selectors[i].Trim())){
                        xPathQuery += string.Format("{0}{1}", (children ? "/" : "//"),  selectors[i].Trim());
                        children = false;
                    }
                }
            }

            return xPathQuery;
        }
    }
}