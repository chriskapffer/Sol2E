using System;
using System.IO;
using Microsoft.VisualStudio.TemplateWizard;
using EnvDTE;

namespace TemplateWizardry
{
    /// <summary>
    /// We have to define an extra wizard class to be able to copy the files,
    /// which are not included in the visual studio project, but are needed as
    /// resources (.dll references or textures of models).
    /// 
    /// It is not working at the moment. It just prints out debug messages.
    /// Also: In order to function at all, this hast to be compiled as a strong
    /// assembly, which then has to be registern in the GAC (global assembly cache).
    /// That of course could be done by the msi. So I'd had to read up, what the wxs
    /// code looks like to do that. But than, if all that succeeds, I have nothing
    /// but this debug messages and have to figure out a way to actually make this
    /// do what it's supposed to be doing, which is copying the files.
    /// 
    /// Workaround: I included the missing files directely, which sould be removed
    /// by the user after project creation. But its enables me to create the template.
    /// </summary>
    public class WizardClass : IWizard
    {
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
            Console.WriteLine("BeforeOpeningFile: " + projectItem.Name);
        }

        public void ProjectFinishedGenerating(Project project)
        {
            Console.WriteLine("ProjectFinishedGenerating: " + project.Name);
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            Console.WriteLine("ProjectItemFinishedGenerating: " + projectItem.Name);
        }

        public void RunFinished()
        {
            Console.WriteLine("RunFinished");
        }

        public void RunStarted(object automationObject, System.Collections.Generic.Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            Console.WriteLine("RunStarted");
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            Console.WriteLine("ShouldAddProjectItem: " + filePath);
            return true;
        }
    }
}
