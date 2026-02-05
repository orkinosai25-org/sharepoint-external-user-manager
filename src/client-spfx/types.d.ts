// Global type definitions for the project
declare module '*.module.scss' {
  const classes: { [key: string]: string };
  export = classes;
}

declare module '*.scss' {
  const content: string;
  export = content;
}

declare module '*.css' {
  const content: string;
  export = content;
}