using System.Collections.Generic;

namespace PQBI.PQS;


public record TagTreeRootDto(List<TagDtoV2> Tags);

public record TagDtoV2(string TagName, List<LabelDtoV2> Labels);

public record LabelDtoV2(string Label, List<ComponentDto> Components);
