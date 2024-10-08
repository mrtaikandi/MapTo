## Release 0.9

### New Rules

 Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
 MT2001  | Mapping  | Warning  | Possible unintentional ignoring the converter additional parameter.
 MT3001  | Mapping  | Error    | Missing partial keyword on target class.
 MT3002  | Mapping  | Error    | Missing constructor on the target class.
 MT3003  | Mapping  | Error    | PropertyTypeConverterAttribute or IgnorePropertyAttribute is required.
 MT3004  | Mapping  | Error    | PropertyTypeConverter method not found in the target class.
 MT3005  | Mapping  | Error    | PropertyTypeConverter method is not static.
 MT3006  | Mapping  | Error    | PropertyTypeConverter method return type is incorrect.
 MT3007  | Mapping  | Error    | PropertyTypeConverter method parameter type is incorrect.
 MT3008  | Mapping  | Error    | PropertyTypeConverter method additional parameter type is incorrect.
 MT3009  | Mapping  | Error    | PropertyTypeConverter method parameter has inconsistent nullability annotation.
 MT3010  | Mapping  | Error    | A suitable mapping type in nested property not found.
 MT3011  | Mapping  | Error    | Cannot create a map for the target type because of self-referencing argument in the constructor.
 MT3012  | Mapping  | Error    | Before or after map method not found.
 MT3013  | Mapping  | Error    | Before or after map method has invalid parameter.
 MT3014  | Mapping  | Error    | Before or after map method has invalid return type.
 MT3015  | Mapping  | Error    | Before or after map method parameter is missing.
 MT3016  | Mapping  | Error    | Before or after map method parameter has inconsistent nullability annotation.
 MT3017  | Mapping  | Error    | Before or after map method return type has inconsistent nullability annotation.
 MT3018  | Mapping  | Error    | After map method does not have correct number of parameters.
 MT3019  | Mapping  | Error    | The source enum member is not found in the target enum.
 MT3020  | Mapping  | Error    | The target enum member is not found in the source enum.
 MT3021  | Mapping  | Error    | The 'IgnoreEnumMemberAttributeIgnoreEnumMemberAttribute' cannot have arguments when applied to an enum member.
 MT3022  | Mapping  | Error    | The 'IgnoreEnumMemberAttributeIgnoreEnumMemberAttribute' must have an argument when applied to an enum or class.
 MT3023  | Mapping  | Error    | The 'MappingConfiguration' method is not declared correctly.