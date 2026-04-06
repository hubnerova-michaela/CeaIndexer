using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Globalization;
using CeaIndexer.FilterModels;
using CeaIndexer.Models;

namespace CeaIndexer.Helpers
{
    public static class MetadataQueryBuilder
    {
        public static Expression<Func<FileEntry, bool>> BuildQuery(ConditionGroup rootGroup)
        {
            var parameter = Expression.Parameter(typeof(FileEntry), "f");
            var body = BuildGroupExpression(rootGroup, parameter);

            if (body == null) return f => true;

            return Expression.Lambda<Func<FileEntry, bool>>(body, parameter);
        }

        private static Expression BuildGroupExpression(ConditionGroup group, ParameterExpression fileParam)
        {
            Expression currentExpr = null;

            foreach (var child in group.Children)
            {
                Expression childExpr = null;

                if (child is ConditionGroup subGroup)
                {
                    childExpr = BuildGroupExpression(subGroup, fileParam);
                }
                else if (child is ConditionRule rule)
                {

                    bool isErxRequired = rule.Category == RuleCategory.Quantity &&
                                         rule.Operator != RelationalOperator.Exists &&
                                         rule.Operator != RelationalOperator.NotExists;

                    if (!isErxRequired)
                    {
                        childExpr = BuildRuleExpression(rule, fileParam);
                    }
                }

                if (childExpr == null) continue;

                if (currentExpr == null)
                    currentExpr = childExpr;
                else
                    currentExpr = group.LogicOperator == LogicalOperator.And
                        ? Expression.AndAlso(currentExpr, childExpr)
                        : Expression.OrElse(currentExpr, childExpr);
            }

            return currentExpr;
        }

        private static Expression BuildRuleExpression(ConditionRule rule, ParameterExpression fileParam)
        {

            if (rule.Operator != RelationalOperator.Exists && rule.Operator != RelationalOperator.NotExists)
            {
                if (string.IsNullOrWhiteSpace(rule.TargetProperty) || string.IsNullOrWhiteSpace(rule.Value))
                    return null;
            }

            // 1. SOUBOR
            if (rule.Category == RuleCategory.File)
            {
                var propertyExpr = Expression.Property(fileParam, rule.TargetProperty);
                return BuildCondition(propertyExpr, rule.Operator, rule.Value);
            }

            // 2. MĚŘÍCÍ BOD
            else if (rule.Category == RuleCategory.Device)
            {
                var mpParam = Expression.Parameter(typeof(MeasurePoint), "m");
                var propertyExpr = Expression.Property(mpParam, rule.TargetProperty);
                var conditionExpr = BuildCondition(propertyExpr, rule.Operator, rule.Value);
                if (conditionExpr == null) return null;

                var innerLambda = Expression.Lambda(conditionExpr, mpParam);
                return CallAny(fileParam, "MeasurePoints", typeof(MeasurePoint), innerLambda);
            }

            // 3. ARCHIV 
            else if (rule.Category == RuleCategory.Archive)
            {
                var aParam = Expression.Parameter(typeof(Archive), "a");
                var propertyExpr = Expression.Property(aParam, rule.TargetProperty);
                var conditionExpr = BuildCondition(propertyExpr, rule.Operator, rule.Value);
                if (conditionExpr == null) return null;

                var aLambda = Expression.Lambda(conditionExpr, aParam);

                var mpParam = Expression.Parameter(typeof(MeasurePoint), "m");
                var aAnyExpr = CallAny(mpParam, "Archives", typeof(Archive), aLambda);
                var mpLambda = Expression.Lambda(aAnyExpr, mpParam);

                return CallAny(fileParam, "MeasurePoints", typeof(MeasurePoint), mpLambda);
            }

            // 4. VELIČINA - POUZE EXISTENCE
            else if (rule.Category == RuleCategory.Quantity &&
                    (rule.Operator == RelationalOperator.Exists || rule.Operator == RelationalOperator.NotExists))
            {
                // q => q.Name == rule.TargetProperty
                var qParam = Expression.Parameter(typeof(QuantityItem), "q");
                var qProp = Expression.Property(qParam, "Name");
                var qVal = Expression.Constant(rule.TargetProperty);
                var qCond = Expression.Equal(qProp, qVal);
                var qLambda = Expression.Lambda(qCond, qParam);

                // a => a.Quantities.Any(...)
                var aParam = Expression.Parameter(typeof(Archive), "a");
                var aAnyExpr = CallAny(aParam, "Quantities", typeof(QuantityItem), qLambda);
                var aLambda = Expression.Lambda(aAnyExpr, aParam);

                // m => m.Archives.Any(...)
                var mpParam = Expression.Parameter(typeof(MeasurePoint), "m");
                var mpAnyExpr = CallAny(mpParam, "Archives", typeof(Archive), aLambda);
                var mpLambda = Expression.Lambda(mpAnyExpr, mpParam);

                // f.MeasurePoints.Any(...)
                Expression finalExpr = CallAny(fileParam, "MeasurePoints", typeof(MeasurePoint), mpLambda);

                // Pokud chceme "Neexistuje", celou podmínku znegujeme (přidáme ! na začátek)
                if (rule.Operator == RelationalOperator.NotExists)
                {
                    finalExpr = Expression.Not(finalExpr);
                }

                return finalExpr;
            }

            return null;
        }


        private static Expression CallAny(Expression collectionOwner, string collectionName, Type itemType, LambdaExpression innerLambda)
        {
            var collectionProp = Expression.Property(collectionOwner, collectionName);
            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(itemType);
            return Expression.Call(anyMethod, collectionProp, innerLambda);
        }

        // UNIVERZÁLNÍ POROVNÁVAČ DAT
        private static Expression BuildCondition(MemberExpression propertyExpr, RelationalOperator op, string stringValue)
        {
            try
            {
                Type propertyType = propertyExpr.Type;
                Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                object parsedValue = null;

                if (underlyingType == typeof(string)) parsedValue = stringValue;
                else if (underlyingType == typeof(double)) parsedValue = double.Parse(stringValue, CultureInfo.InvariantCulture);
                else if (underlyingType == typeof(int)) parsedValue = int.Parse(stringValue);
                else if (underlyingType == typeof(DateTime)) parsedValue = DateTime.Parse(stringValue);
                else return null;

                Expression constantExpr = Expression.Constant(parsedValue, underlyingType);

                if (propertyType != underlyingType)
                {
                    constantExpr = Expression.Convert(constantExpr, propertyType);
                }

                switch (op)
                {
                    case RelationalOperator.Equals:
                        if (underlyingType == typeof(DateTime))
                        {
                            DateTime dateValue = (DateTime)parsedValue;
                            DateTime startDate = dateValue.Date; 
                            DateTime endDate = startDate.AddDays(1);

                            Expression startExpr = Expression.Constant(startDate, underlyingType);
                            Expression endExpr = Expression.Constant(endDate, underlyingType);


                            if (propertyType != underlyingType)
                            {
                                startExpr = Expression.Convert(startExpr, propertyType);
                                endExpr = Expression.Convert(endExpr, propertyType);
                            }


                            Expression greaterThanOrEqual = Expression.GreaterThanOrEqual(propertyExpr, startExpr);

                            Expression lessThan = Expression.LessThan(propertyExpr, endExpr);

                            return Expression.AndAlso(greaterThanOrEqual, lessThan);
                        }
                        return Expression.Equal(propertyExpr, constantExpr);
                    case RelationalOperator.GreaterThan: return Expression.GreaterThan(propertyExpr, constantExpr);
                    case RelationalOperator.LessThan: return Expression.LessThan(propertyExpr, constantExpr);
                    case RelationalOperator.Contains:
                        if (underlyingType == typeof(string))
                        {
                            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                            var nullCheck = Expression.NotEqual(propertyExpr, Expression.Constant(null, typeof(string)));
                            var containsCall = Expression.Call(propertyExpr, containsMethod, constantExpr);
                            return Expression.AndAlso(nullCheck, containsCall);
                        }
                        return null;
                    default: return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}