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
                else if (child is ConditionRule rule && rule.Category != RuleCategory.Quantity)
                {
                    childExpr = BuildRuleExpression(rule, fileParam);
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
            if (string.IsNullOrWhiteSpace(rule.TargetProperty) || string.IsNullOrWhiteSpace(rule.Value))
                return null;

            if (rule.Category == RuleCategory.File)
            {

                var propertyExpr = Expression.Property(fileParam, rule.TargetProperty);
                return BuildCondition(propertyExpr, rule.Operator, rule.Value);
            }
            else if (rule.Category == RuleCategory.Device)
            {


                // parametr pro vnitřní lambda výraz (m =>)
                var measurePointParam = Expression.Parameter(typeof(MeasurePoint), "m");

                // vlastnost na MeasurePoint (např. m.DeviceType)
                var propertyExpr = Expression.Property(measurePointParam, rule.TargetProperty);

                // sestavíme podmínku (m.DeviceType == "____")
                var conditionExpr = BuildCondition(propertyExpr, rule.Operator, rule.Value);
                if (conditionExpr == null) return null;

                // uzavřeme do vnitřní lambdy: m => m.DeviceType == "____"
                var innerLambda = Expression.Lambda(conditionExpr, measurePointParam);

                // zavoláme funkci .Any(...) na f.MeasurePoints
                var measurePointsExpr = Expression.Property(fileParam, "MeasurePoints");
                var anyMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(MeasurePoint));

                return Expression.Call(anyMethod, measurePointsExpr, innerLambda);
            }

            return null;
        }

        // UNIVERZÁLNÍ POROVNÁVAČ DAT (Texty, Čísla, Datumy)
        private static Expression BuildCondition(MemberExpression propertyExpr, RelationalOperator op, string stringValue)
        {
            try
            {

                Type propertyType = propertyExpr.Type;
                Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                object parsedValue = null;

                if (underlyingType == typeof(string))
                {
                    parsedValue = stringValue;
                }
                else if (underlyingType == typeof(double))
                {
                    parsedValue = double.Parse(stringValue, CultureInfo.InvariantCulture);
                }
                else if (underlyingType == typeof(int))
                {
                    parsedValue = int.Parse(stringValue);
                }
                else if (underlyingType == typeof(DateTime))
                {
                    parsedValue = DateTime.Parse(stringValue);
                }
                else
                {
                    return null;
                }


                Expression constantExpr = Expression.Constant(parsedValue, underlyingType);

                if (propertyType != underlyingType)
                {
                    constantExpr = Expression.Convert(constantExpr, propertyType);
                }

                switch (op)
                {
                    case RelationalOperator.Equals:
                        return Expression.Equal(propertyExpr, constantExpr);

                    case RelationalOperator.GreaterThan:
                        return Expression.GreaterThan(propertyExpr, constantExpr);

                    case RelationalOperator.LessThan:
                        return Expression.LessThan(propertyExpr, constantExpr);

                    case RelationalOperator.Contains:
                        if (underlyingType == typeof(string))
                        {
                            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                            var nullCheck = Expression.NotEqual(propertyExpr, Expression.Constant(null, typeof(string)));
                            var containsCall = Expression.Call(propertyExpr, containsMethod, constantExpr);
                            return Expression.AndAlso(nullCheck, containsCall);
                        }
                        return null;

                    default:
                        return null;
                }
            }
            catch
            {

                return null;
            }
        }
    }
}