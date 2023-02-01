using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace RevitAPIPipeCreateParameter
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));           

            using (Transaction ts = new Transaction(doc, "Add parameter"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Длина с запасом", categorySet, BuiltInParameterGroup.PG_LENGTH, true);
                ts.Commit();
            }
           
            IList<Reference> selectedElementRefList = uidoc.Selection.PickObjects(ObjectType.Element, new PipeFilter(), "Выберете элементы");
            var pipeList = new List<double>();            
            foreach (var selectedElement in selectedElementRefList)
            {
                Pipe pipe = doc.GetElement(selectedElement) as Pipe;
                Parameter length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);              
                double stock = 1.1;
                double lengthStock = length.AsDouble() * stock;
               
                using(Transaction ts=new Transaction(doc, "Set parameters"))
                {
                    ts.Start();

                    var pipeParameter = pipe as Pipe;
                    Parameter lengthParameter = pipeParameter.LookupParameter("Длина с запасом");
                    lengthParameter.Set(lengthStock);

                    ts.Commit();
                }

            }
           
            return Result.Succeeded;
        }

        private void CreateSharedParameter(Application application,
            Document doc, string parameterName, CategorySet categorySet,
            BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                return;
            }

            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);

            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);
        }
    }
}
